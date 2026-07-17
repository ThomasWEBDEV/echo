"""
Script Blender (interface graphique) — Injection animation Jump In Place (retargeting)
Blender 5.2 — Scripting > Open > inject_jump_blender.py > Run Script

Approche : retargeting via contraintes Copy Transforms.
Au lieu de transférer les valeurs brutes de keyframes (qui dépendent de la
rest pose de chaque armature), on copie les transforms en espace monde frame
par frame. Ça corrige les différences de rest pose et d'axes entre FBX et GLB.

Étapes automatisées :
  1. Vide la scène
  2. Importe raw_assets/character/ybot.glb (17 animations, sans Jump)
  3. Importe Downloads/Jump.fbx
  4. Ajoute des contraintes Copy Transforms sur chaque bone GLB → bone FBX
  5. Bake l'animation (visual keying) sur l'armature GLB
  6. Supprime les contraintes et les objets FBX temporaires
  7. Renomme l'action bakée en "Jump", la pousse en NLA
  8. Exporte le GLB final (18 animations)
  9. Copie dans godot_project
"""

import bpy
import shutil

GLB_SOURCE = r"C:\Users\thoma\Desktop\echo\raw_assets\character\ybot.glb"
GLB_GODOT  = r"C:\Users\thoma\Desktop\echo\godot_project\assets\characters\ybot.glb"
FBX_JUMP   = r"C:\Users\thoma\Downloads\Jump.fbx"

# ─── 1. Vider la scène ───────────────────────────────────────────────────────
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for col in [bpy.data.meshes, bpy.data.armatures, bpy.data.actions,
            bpy.data.materials, bpy.data.cameras, bpy.data.lights]:
    for item in list(col):
        col.remove(item, do_unlink=True)
print("[ECHO] Scène vidée.")

# ─── 2. Importer le GLB principal ────────────────────────────────────────────
bpy.ops.import_scene.gltf(filepath=GLB_SOURCE)
glb_arm = next((o for o in bpy.data.objects if o.type == 'ARMATURE'), None)
assert glb_arm, "[ERREUR] Armature GLB non trouvée."
print(f"[ECHO] Armature GLB : '{glb_arm.name}'")

glb_action_names = {a.name for a in bpy.data.actions}

# Supprimer un éventuel Jump existant
existing_jump = bpy.data.actions.get("Jump")
if existing_jump:
    bpy.data.actions.remove(existing_jump, do_unlink=True)
    glb_action_names.discard("Jump")
    print("[WARN] Action 'Jump' existante supprimée.")

# ─── 3. Importer le FBX Jump In Place ────────────────────────────────────────
bpy.ops.import_scene.fbx(filepath=FBX_JUMP, automatic_bone_orientation=False)
fbx_arm = next(
    (o for o in bpy.data.objects if o.type == 'ARMATURE' and o.name != glb_arm.name),
    None
)
assert fbx_arm, "[ERREUR] Armature FBX non trouvée."
print(f"[ECHO] Armature FBX : '{fbx_arm.name}'")

fbx_action = next((a for a in bpy.data.actions if a.name not in glb_action_names), None)
assert fbx_action, "[ERREUR] Aucune nouvelle action FBX trouvée."
print(f"[ECHO] Action FBX brute : '{fbx_action.name}'")

# ─── 4. Associer l'action FBX à l'armature FBX ───────────────────────────────
# Nécessaire pour que le bake évalue l'animation FBX à chaque frame.
if fbx_arm.animation_data is None:
    fbx_arm.animation_data_create()
fbx_arm.animation_data.action = fbx_action

# Blender 5.2 slotted actions : associer le bon slot si disponible
if hasattr(fbx_arm.animation_data, 'action_slot') and hasattr(fbx_action, 'slots'):
    for slot in fbx_action.slots:
        try:
            fbx_arm.animation_data.action_slot = slot
            print(f"[ECHO] Slot FBX assigné : '{slot.name}'")
            break
        except Exception:
            pass

# Plage de frames
try:
    frame_start = int(fbx_action.frame_range[0])
    frame_end   = int(fbx_action.frame_range[1])
except Exception:
    frame_start, frame_end = 1, 60
print(f"[ECHO] Frames : {frame_start} → {frame_end} ({frame_end - frame_start + 1} frames)")

bpy.context.scene.frame_start = frame_start
bpy.context.scene.frame_end   = frame_end

# ─── 5. Retargeting : Copy Transforms GLB ← FBX ─────────────────────────────
bpy.ops.object.select_all(action='DESELECT')
glb_arm.select_set(True)
bpy.context.view_layer.objects.active = glb_arm
bpy.ops.object.mode_set(mode='POSE')

bones_ok = 0
bones_ko = 0
for glb_bone in glb_arm.pose.bones:
    if glb_bone.name not in fbx_arm.pose.bones:
        bones_ko += 1
        continue
    ct = glb_bone.constraints.new('COPY_TRANSFORMS')
    ct.name = "RETARGET_JUMP"
    ct.target    = fbx_arm
    ct.subtarget = glb_bone.name
    bones_ok += 1

bpy.ops.object.mode_set(mode='OBJECT')
print(f"[ECHO] Contraintes Copy Transforms : {bones_ok} os couplés, {bones_ko} os ignorés.")
assert bones_ok > 0, "[ERREUR] Aucun os couplé — vérifier les noms de bones."

# ─── 6. Bake (visual keying) sur l'armature GLB ──────────────────────────────
bpy.ops.object.select_all(action='DESELECT')
glb_arm.select_set(True)
bpy.context.view_layer.objects.active = glb_arm
bpy.ops.object.mode_set(mode='POSE')
bpy.ops.pose.select_all(action='SELECT')

print(f"[ECHO] Bake en cours ({frame_end - frame_start + 1} frames)…")
bpy.ops.nla.bake(
    frame_start=frame_start,
    frame_end=frame_end,
    step=1,
    only_selected=False,
    visual_keying=True,       # bake la pose visuelle (contraintes appliquées)
    clear_constraints=True,   # supprime RETARGET_JUMP après le bake
    clear_parents=False,
    use_current_action=False, # crée une nouvelle action
    bake_types={'POSE'},
)
bpy.ops.object.mode_set(mode='OBJECT')

# Récupérer l'action bakée
baked_action = glb_arm.animation_data.action
assert baked_action, "[ERREUR] Le bake n'a pas produit d'action."
baked_action.name = "Jump"
print(f"[ECHO] Action bakée : '{baked_action.name}'")

# ─── 7. Supprimer les objets FBX temporaires ─────────────────────────────────
glb_arm.animation_data.action = None  # détacher pour le NLA push

fbx_objects = [
    o for o in bpy.data.objects
    if o.type == 'MESH' and any(m.type == 'ARMATURE' and m.object == fbx_arm
                                 for m in o.modifiers)
]
fbx_objects.append(fbx_arm)

bpy.ops.object.select_all(action='DESELECT')
for obj in fbx_objects:
    obj.select_set(True)
bpy.ops.object.delete(use_global=False)
print(f"[ECHO] {len(fbx_objects)} objet(s) FBX supprimé(s).")

# Supprimer aussi l'action FBX brute (on garde uniquement "Jump" bakée)
if fbx_action.name in bpy.data.actions:
    bpy.data.actions.remove(fbx_action, do_unlink=True)
    print("[ECHO] Action FBX brute supprimée.")

# ─── 8. Pousser "Jump" en NLA sur l'armature GLB ─────────────────────────────
if not glb_arm.animation_data:
    glb_arm.animation_data_create()

# Nettoyer les anciens tracks Jump
for track in list(glb_arm.animation_data.nla_tracks):
    for strip in list(track.strips):
        if strip.name == "Jump" or (strip.action and strip.action.name == "Jump"):
            track.strips.remove(strip)
    if not track.strips:
        glb_arm.animation_data.nla_tracks.remove(track)

jump_action = bpy.data.actions.get("Jump")
assert jump_action, "[ERREUR] Action 'Jump' introuvable avant export."

nla_track = glb_arm.animation_data.nla_tracks.new()
nla_track.name = "Jump"
nla_strip = nla_track.strips.new(name="Jump", start=frame_start, action=jump_action)
glb_arm.animation_data.action = None
print(f"[ECHO] NLA track 'Jump' : {nla_strip.frame_start:.0f}→{nla_strip.frame_end:.0f} frames.")

# ─── 9. Vérification finale ───────────────────────────────────────────────────
print("[ECHO] Actions dans le fichier :")
for a in sorted(bpy.data.actions, key=lambda x: x.name):
    print(f"  - '{a.name}'")
total = len(bpy.data.actions)
print(f"[ECHO] Total : {total} actions (attendu : 18)")

# ─── 10. Exporter en GLB ─────────────────────────────────────────────────────
bpy.ops.export_scene.gltf(
    filepath=GLB_SOURCE,
    export_format='GLB',
    export_animations=True,
    export_anim_single_armature=True,
    export_current_frame=False,
    export_rest_position_armature=False,
)
print(f"[ECHO] GLB exporté → {GLB_SOURCE}")

# ─── 11. Copier vers godot_project ───────────────────────────────────────────
shutil.copy2(GLB_SOURCE, GLB_GODOT)
print(f"[ECHO] GLB copié   → {GLB_GODOT}")
print("[ECHO] Terminé. Dans Godot : clic droit sur ybot.glb > Reimport.")
