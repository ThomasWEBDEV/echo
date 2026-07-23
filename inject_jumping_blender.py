"""
Script Blender — Injection animation Jump (marche/idle) + renommage RunningJump
Blender 5.2 — Scripting > Open > inject_jumping_blender.py > Run Script

Ce script :
  1. Importe le GLB actuel (18 animations, dont "Jump" = saut en course)
  2. Renomme l'action "Jump" en "RunningJump"
  3. Importe Downloads/Jumping.fbx (saut marche/idle)
  4. Retargeting COPY_TRANSFORMS + bake worldspace → action "Jump" correcte
  5. Exporte le GLB final (19 animations : "Jump" + "RunningJump" + 17 autres)
  6. Copie dans godot_project

Résultat attendu : 19 actions dans la console.
"""

import bpy
import shutil

GLB_SOURCE   = r"C:\Users\thoma\Desktop\echo\raw_assets\character\ybot.glb"
GLB_GODOT    = r"C:\Users\thoma\Desktop\echo\godot_project\assets\characters\ybot.glb"
FBX_JUMPING  = r"C:\Users\thoma\Downloads\Jumping.fbx"

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
print(f"[ECHO] Actions GLB chargées : {len(glb_action_names)}")

# ─── 3. Renommer "Jump" → "RunningJump" ──────────────────────────────────────
jump_action = bpy.data.actions.get("Jump")
assert jump_action, "[ERREUR] Action 'Jump' introuvable dans le GLB — vérifier le GLB source."

jump_action.name = "RunningJump"
print("[ECHO] Action 'Jump' renommée en 'RunningJump'.")

# Renommer aussi la NLA track correspondante
if glb_arm.animation_data:
    for track in glb_arm.animation_data.nla_tracks:
        if track.name == "Jump":
            track.name = "RunningJump"
            print("[ECHO] NLA track 'Jump' renommée en 'RunningJump'.")
        for strip in track.strips:
            if strip.name == "Jump":
                strip.name = "RunningJump"

# Mettre à jour l'ensemble des noms connus
glb_action_names = {a.name for a in bpy.data.actions}

# ─── 4. Supprimer un "Jump" résiduel si présent ──────────────────────────────
existing_jump = bpy.data.actions.get("Jump")
if existing_jump:
    bpy.data.actions.remove(existing_jump, do_unlink=True)
    glb_action_names.discard("Jump")
    print("[WARN] Action 'Jump' résiduelle supprimée.")

# ─── 5. Importer le FBX Jumping (marche/idle) ────────────────────────────────
bpy.ops.import_scene.fbx(filepath=FBX_JUMPING, automatic_bone_orientation=False)
fbx_arm = next(
    (o for o in bpy.data.objects if o.type == 'ARMATURE' and o != glb_arm),
    None
)
assert fbx_arm, "[ERREUR] Armature FBX non trouvée."
print(f"[ECHO] Armature FBX : '{fbx_arm.name}'")

fbx_action = next((a for a in bpy.data.actions if a.name not in glb_action_names), None)
assert fbx_action, "[ERREUR] Aucune nouvelle action FBX trouvée."
print(f"[ECHO] Action FBX brute : '{fbx_action.name}'")

# ─── 6. Assigner l'action au FBX armature ────────────────────────────────────
if not fbx_arm.animation_data:
    fbx_arm.animation_data_create()
fbx_arm.animation_data.action = fbx_action
if hasattr(fbx_arm.animation_data, 'action_slot') and fbx_action.slots:
    fbx_arm.animation_data.action_slot = fbx_action.slots[0]
    print(f"[ECHO] Slot FBX assigné : '{fbx_action.slots[0].identifier}'")

frame_start = int(fbx_action.frame_range[0])
frame_end   = int(fbx_action.frame_range[1])
print(f"[ECHO] Frames Jumping : {frame_start} → {frame_end}")

bpy.context.scene.frame_start = frame_start
bpy.context.scene.frame_end   = frame_end

# ─── 7. Muter les NLA tracks du GLB (évite interférence pendant le bake) ─────
bpy.context.view_layer.objects.active = glb_arm
glb_arm.select_set(True)
if glb_arm.animation_data:
    for track in glb_arm.animation_data.nla_tracks:
        track.mute = True
print("[ECHO] NLA tracks GLB mutés temporairement.")

# ─── 8. Ajouter contraintes COPY_TRANSFORMS sur chaque bone GLB ──────────────
bpy.ops.object.mode_set(mode='POSE')
matched = 0
for pose_bone in glb_arm.pose.bones:
    if pose_bone.name in fbx_arm.pose.bones:
        ct = pose_bone.constraints.new(type='COPY_TRANSFORMS')
        ct.name = "RETARGET_JUMP"
        ct.target = fbx_arm
        ct.subtarget = pose_bone.name
        matched += 1
bpy.ops.object.mode_set(mode='OBJECT')
print(f"[ECHO] {matched} bones avec contrainte COPY_TRANSFORMS.")
assert matched > 0, "[ERREUR] Aucun bone correspondant trouvé entre GLB et FBX."

# ─── 9. Bake worldspace → nouvelle action "Jump" ─────────────────────────────
bpy.context.view_layer.objects.active = glb_arm
bpy.ops.object.mode_set(mode='POSE')
bpy.ops.pose.select_all(action='SELECT')

bpy.ops.nla.bake(
    frame_start=frame_start,
    frame_end=frame_end,
    step=1,
    only_selected=False,
    visual_keying=True,
    clear_constraints=True,
    clear_parents=False,
    use_current_action=False,
    bake_types={'POSE'},
)
bpy.ops.object.mode_set(mode='OBJECT')
print("[ECHO] Bake worldspace terminé.")

# ─── 10. Renommer l'action cuite en "Jump" ────────────────────────────────────
baked_action = glb_arm.animation_data.action
assert baked_action, "[ERREUR] Pas d'action cuite sur le GLB armature après bake."
baked_action.name = "Jump"
glb_arm.animation_data.action = None
print("[ECHO] Action cuite renommée en 'Jump'.")

# ─── 11. Remettre les NLA tracks GLB actifs ──────────────────────────────────
if glb_arm.animation_data:
    for track in glb_arm.animation_data.nla_tracks:
        track.mute = False

# ─── 12. Pousser "Jump" en NLA track ─────────────────────────────────────────
if not glb_arm.animation_data:
    glb_arm.animation_data_create()

# Nettoyer les anciens tracks "Jump" résiduels
for track in list(glb_arm.animation_data.nla_tracks):
    for strip in list(track.strips):
        if strip.name == "Jump" or (strip.action and strip.action.name == "Jump"):
            track.strips.remove(strip)
    if not track.strips:
        glb_arm.animation_data.nla_tracks.remove(track)

jump_action = bpy.data.actions["Jump"]
nla_track   = glb_arm.animation_data.nla_tracks.new()
nla_track.name = "Jump"
nla_strip = nla_track.strips.new(name="Jump", start=frame_start, action=jump_action)
glb_arm.animation_data.action = None
print(f"[ECHO] NLA track 'Jump' créé : {nla_strip.frame_start:.0f}→{nla_strip.frame_end:.0f}")

# ─── 13. Supprimer les objets FBX temporaires ────────────────────────────────
fbx_objects = [o for o in bpy.data.objects
               if o.type == 'ARMATURE' and o != glb_arm]
for obj in list(bpy.data.objects):
    for mod in obj.modifiers:
        if mod.type == 'ARMATURE' and mod.object in fbx_objects:
            if obj not in fbx_objects:
                fbx_objects.append(obj)
            break

if fbx_objects:
    bpy.ops.object.select_all(action='DESELECT')
    for obj in fbx_objects:
        if obj.name in bpy.data.objects:
            obj.select_set(True)
    bpy.ops.object.delete(use_global=False)
    print(f"[ECHO] {len(fbx_objects)} objet(s) FBX supprimé(s).")
else:
    print("[WARN] Aucun objet FBX à supprimer.")

# ─── 14. Vérification avant export ───────────────────────────────────────────
print("\n[ECHO] Actions finales :")
for a in sorted(bpy.data.actions, key=lambda x: x.name):
    print(f"  - '{a.name}'")
total_actions = len(bpy.data.actions)
print(f"[ECHO] Total : {total_actions} actions (attendu : 19)")
assert bpy.data.actions.get("Jump"),        "[ERREUR] Action 'Jump' absente — annulation."
assert bpy.data.actions.get("RunningJump"), "[ERREUR] Action 'RunningJump' absente — annulation."

# ─── 15. Exporter en GLB ─────────────────────────────────────────────────────
bpy.ops.export_scene.gltf(
    filepath=GLB_SOURCE,
    export_format='GLB',
    export_animations=True,
    export_anim_single_armature=True,
    export_current_frame=False,
    export_rest_position_armature=False,
)
print(f"[ECHO] GLB exporté → {GLB_SOURCE}")

# ─── 16. Copier vers godot_project ───────────────────────────────────────────
shutil.copy2(GLB_SOURCE, GLB_GODOT)
print(f"[ECHO] GLB copié   → {GLB_GODOT}")
print("[ECHO] Terminé. Dans Godot : clic droit sur ybot.glb > Reimport.")
