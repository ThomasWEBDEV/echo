"""
Script Blender (interface graphique) — Injection animation Jump In Place
Blender 5.2 — Scripting > Open > inject_jump_blender.py > Run Script

Étapes automatisées :
  1. Vide la scène
  2. Importe raw_assets/character/ybot.glb (17 animations, sans Jump)
  3. Importe Downloads/Jumping.fbx
  4. Prend la nouvelle action, la renomme "Jump", la pousse en NLA
  5. Supprime les objets FBX temporaires
  6. Exporte le GLB final (18 animations)
  7. Copie dans godot_project
"""

import bpy
import shutil

GLB_SOURCE = r"C:\Users\thoma\Desktop\echo\raw_assets\character\ybot.glb"
GLB_GODOT  = r"C:\Users\thoma\Desktop\echo\godot_project\assets\characters\ybot.glb"
FBX_JUMP   = r"C:\Users\thoma\Downloads\Jumping.fbx"

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
print(f"[ECHO] GLB importé : {GLB_SOURCE}")

glb_arm = next((o for o in bpy.data.objects if o.type == 'ARMATURE'), None)
assert glb_arm, "[ERREUR] Aucune armature trouvée après import GLB."
print(f"[ECHO] Armature GLB : '{glb_arm.name}'")

# Snapshot des actions existantes (17 animations originales)
glb_action_names = {a.name for a in bpy.data.actions}
print(f"[ECHO] {len(glb_action_names)} actions existantes dans le GLB.")

# Vérifier qu'il n'y a pas déjà un "Jump"
existing_jump = next((a for a in bpy.data.actions if a.name == "Jump"), None)
if existing_jump:
    print("[WARN] Une action 'Jump' existe déjà — elle sera remplacée.")
    bpy.data.actions.remove(existing_jump, do_unlink=True)
    glb_action_names.discard("Jump")

# ─── 3. Importer le FBX Jump In Place ────────────────────────────────────────
bpy.ops.import_scene.fbx(filepath=FBX_JUMP, automatic_bone_orientation=False)
print(f"[ECHO] FBX importé : {FBX_JUMP}")

# ─── 4. Identifier la nouvelle action créée par le FBX ───────────────────────
fbx_action = next((a for a in bpy.data.actions if a.name not in glb_action_names), None)
assert fbx_action, "[ERREUR] Aucune nouvelle action trouvée après import FBX."
print(f"[ECHO] Action FBX : '{fbx_action.name}'")

fbx_action.name = "Jump"
print("[ECHO] Action renommée en 'Jump'.")

# ─── 5. Vérifier l'absence de root motion dans l'animation ───────────────────
# Blender 5.x : fcurves peut être dans layers[0].strips[0] ou directement sur l'action
def get_fcurves(action):
    # Nouveau système Layered Actions (Blender 4.4+)
    if hasattr(action, 'layers') and action.layers:
        for layer in action.layers:
            if hasattr(layer, 'strips'):
                for strip in layer.strips:
                    if hasattr(strip, 'fcurves'):
                        return list(strip.fcurves)
    # Ancien système (Blender < 4.4)
    if hasattr(action, 'fcurves'):
        return list(action.fcurves)
    return []

fcurves = get_fcurves(fbx_action)
print(f"[ECHO] {len(fcurves)} fcurves trouvées dans l'action Jump.")
root_drift = None
for fc in fcurves:
    if "Hips" in fc.data_path and "location" in fc.data_path and fc.array_index == 0:
        if fc.keyframe_points:
            start_val = fc.keyframe_points[0].co[1]
            end_val   = fc.keyframe_points[-1].co[1]
            root_drift = abs(end_val - start_val)
            print(f"[ECHO] Drift X Hips : {root_drift:.4f} (start={start_val:.4f}, end={end_val:.4f})")
        break
if root_drift is not None and root_drift > 0.01:
    print(f"[WARN] Root motion détecté (drift={root_drift:.4f}) — vérifier que 'Jump In Place' a bien été téléchargé !")
elif root_drift is not None:
    print(f"[ECHO] OK — pas de root motion significatif.")
else:
    print("[ECHO] Hips non trouvé dans les fcurves — vérification root motion ignorée.")

# ─── 6. Pousser "Jump" en NLA strip sur l'armature GLB ───────────────────────
bpy.ops.object.select_all(action='DESELECT')
glb_arm.select_set(True)
bpy.context.view_layer.objects.active = glb_arm

if not glb_arm.animation_data:
    glb_arm.animation_data_create()

# Vérifier s'il existe déjà un track "Jump" en NLA et le supprimer
for track in list(glb_arm.animation_data.nla_tracks):
    for strip in list(track.strips):
        if strip.name == "Jump" or (strip.action and strip.action.name == "Jump"):
            track.strips.remove(strip)
    if not track.strips:
        glb_arm.animation_data.nla_tracks.remove(track)

# API directe — ne nécessite pas l'éditeur NLA actif
jump_action = bpy.data.actions["Jump"]
nla_track = glb_arm.animation_data.nla_tracks.new()
nla_track.name = "Jump"
nla_strip = nla_track.strips.new(name="Jump", start=1, action=jump_action)
glb_arm.animation_data.action = None
print(f"[ECHO] NLA track 'Jump' créé : {nla_strip.frame_start:.0f}→{nla_strip.frame_end:.0f} frames.")

# ─── 7. Supprimer les objets FBX temporaires ─────────────────────────────────
fbx_arm = next((o for o in bpy.data.objects
                if o.type == 'ARMATURE' and o.name != glb_arm.name), None)

fbx_objects = []
if fbx_arm:
    for obj in bpy.data.objects:
        for mod in obj.modifiers:
            if mod.type == 'ARMATURE' and mod.object == fbx_arm:
                fbx_objects.append(obj)
    fbx_objects.append(fbx_arm)

if fbx_objects:
    bpy.ops.object.select_all(action='DESELECT')
    for obj in fbx_objects:
        obj.select_set(True)
    bpy.ops.object.delete(use_global=False)
    print(f"[ECHO] {len(fbx_objects)} objet(s) FBX supprimé(s).")
else:
    print("[WARN] Aucun objet FBX à supprimer.")

# ─── 8. Vérification avant export ────────────────────────────────────────────
print("[ECHO] Actions finales :")
for a in sorted(bpy.data.actions, key=lambda x: x.name):
    print(f"  - '{a.name}'")

assert bpy.data.actions.get("Jump"), "[ERREUR] Action 'Jump' absente avant export — annulation."
total_actions = len(bpy.data.actions)
print(f"[ECHO] Total : {total_actions} actions (attendu : 18)")

# ─── 9. Exporter en GLB ──────────────────────────────────────────────────────
bpy.ops.export_scene.gltf(
    filepath=GLB_SOURCE,
    export_format='GLB',
    export_animations=True,
    export_anim_single_armature=True,
    export_current_frame=False,
    export_rest_position_armature=False,
)
print(f"[ECHO] GLB exporté → {GLB_SOURCE}")

# ─── 10. Copier vers godot_project ───────────────────────────────────────────
shutil.copy2(GLB_SOURCE, GLB_GODOT)
print(f"[ECHO] GLB copié   → {GLB_GODOT}")
print("[ECHO] Terminé. Dans Godot : clic droit sur ybot.glb > Reimport.")
