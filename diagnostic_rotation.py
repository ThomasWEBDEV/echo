"""
Script diagnostic — Analyser les rotations d'armature FBX vs GLB
Blender 5.2 — Scripting > Open > diagnostic_rotation.py > Run Script

Objectif : comprendre pourquoi l'animation Jump joue de côté.
Résultat affiché dans : Window > Toggle System Console
"""

import bpy
import math

GLB_SOURCE = r"C:\Users\thoma\Desktop\echo\raw_assets\character\ybot.glb"
FBX_JUMP   = r"C:\Users\thoma\Downloads\Jump.fbx"

def to_deg(euler):
    return f"({math.degrees(euler.x):.2f}°, {math.degrees(euler.y):.2f}°, {math.degrees(euler.z):.2f}°)"

def get_fcurves(action):
    if hasattr(action, 'layers') and action.layers:
        for layer in action.layers:
            if hasattr(layer, 'strips'):
                for strip in layer.strips:
                    if hasattr(strip, 'fcurves'):
                        return list(strip.fcurves)
    if hasattr(action, 'fcurves'):
        return list(action.fcurves)
    return []

print("\n" + "="*60)
print("DIAGNOSTIC ROTATION — ECHO Jump animation")
print("="*60)

# ─── 1. Vider la scène ───────────────────────────────────────────────────────
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for col in [bpy.data.meshes, bpy.data.armatures, bpy.data.actions,
            bpy.data.materials, bpy.data.cameras, bpy.data.lights]:
    for item in list(col):
        col.remove(item, do_unlink=True)

# ─── 2. Importer le GLB ──────────────────────────────────────────────────────
bpy.ops.import_scene.gltf(filepath=GLB_SOURCE)
glb_arm = next((o for o in bpy.data.objects if o.type == 'ARMATURE'), None)
assert glb_arm, "[ERREUR] Armature GLB non trouvée."

print(f"\n[GLB] Armature : '{glb_arm.name}'")
print(f"[GLB] rotation_mode      : {glb_arm.rotation_mode}")
print(f"[GLB] rotation_euler     : {to_deg(glb_arm.rotation_euler)}")
print(f"[GLB] matrix_world (rot) :")
m = glb_arm.matrix_world.to_3x3()
for row in m:
    print(f"        [{row[0]:+.4f}  {row[1]:+.4f}  {row[2]:+.4f}]")

# Afficher le bone Hips en rest pose (edit mode donne la matrice locale)
if "mixamorig:Hips" in glb_arm.data.bones:
    hips_bone = glb_arm.data.bones["mixamorig:Hips"]
    print(f"[GLB] Hips head (armature space) : {hips_bone.head_local}")

# ─── 3. Importer le FBX ──────────────────────────────────────────────────────
glb_action_names = {a.name for a in bpy.data.actions}
bpy.ops.import_scene.fbx(filepath=FBX_JUMP, automatic_bone_orientation=False)

fbx_arm = next(
    (o for o in bpy.data.objects if o.type == 'ARMATURE' and o.name != glb_arm.name),
    None
)
assert fbx_arm, "[ERREUR] Armature FBX non trouvée."

print(f"\n[FBX] Armature : '{fbx_arm.name}'")
print(f"[FBX] rotation_mode      : {fbx_arm.rotation_mode}")
print(f"[FBX] rotation_euler     : {to_deg(fbx_arm.rotation_euler)}")
print(f"[FBX] matrix_world (rot) :")
m = fbx_arm.matrix_world.to_3x3()
for row in m:
    print(f"        [{row[0]:+.4f}  {row[1]:+.4f}  {row[2]:+.4f}]")

if "mixamorig:Hips" in fbx_arm.data.bones:
    hips_bone = fbx_arm.data.bones["mixamorig:Hips"]
    print(f"[FBX] Hips head (armature space) : {hips_bone.head_local}")

# ─── 4. Analyser les keyframes du Hips dans l'action FBX ─────────────────────
fbx_action = next((a for a in bpy.data.actions if a.name not in glb_action_names), None)
assert fbx_action, "[ERREUR] Aucune nouvelle action FBX trouvée."
print(f"\n[FBX Action] '{fbx_action.name}'")

fcurves = get_fcurves(fbx_action)
print(f"[FBX Action] {len(fcurves)} fcurves au total.")

# Location du Hips
hips_loc = sorted(
    [fc for fc in fcurves if "Hips" in fc.data_path and "location" in fc.data_path],
    key=lambda fc: fc.array_index
)
print(f"\n[Hips location] {len(hips_loc)} axes :")
axis_labels = {0: "X", 1: "Y", 2: "Z"}
for fc in hips_loc:
    kps = fc.keyframe_points
    if kps:
        vals = [kp.co[1] for kp in kps]
        print(f"  axis[{fc.array_index}] ({axis_labels.get(fc.array_index,'?')}) : "
              f"min={min(vals):+.4f}  max={max(vals):+.4f}  "
              f"début={vals[0]:+.4f}  fin={vals[-1]:+.4f}  "
              f"({len(kps)} keyframes)")

# Rotation du Hips
hips_rot = sorted(
    [fc for fc in fcurves if "Hips" in fc.data_path and "rotation" in fc.data_path],
    key=lambda fc: fc.array_index
)
rotation_type = "quaternion" if any("quaternion" in fc.data_path for fc in hips_rot) else "euler"
print(f"\n[Hips rotation] type={rotation_type}, {len(hips_rot)} canaux :")
for fc in hips_rot:
    kps = fc.keyframe_points
    if kps:
        vals = [kp.co[1] for kp in kps]
        print(f"  [{fc.data_path}][{fc.array_index}] : "
              f"min={min(vals):+.4f}  max={max(vals):+.4f}  ({len(kps)} keyframes)")

# ─── 5. Résumé ────────────────────────────────────────────────────────────────
print("\n" + "="*60)
print("RÉSUMÉ")
print("="*60)
print(f"GLB rotation : {to_deg(glb_arm.rotation_euler)}")
print(f"FBX rotation : {to_deg(fbx_arm.rotation_euler)}")

ex, ey, ez = [math.degrees(v) for v in fbx_arm.rotation_euler]
if abs(abs(ex) - 90) < 1:
    print(f"=> Différence détectée sur l'axe X : {ex:.1f}°")
    print("=> CAUSE PROBABLE : espace Z-up (FBX) vs Y-up (GLB)")
    print("=> CORRECTION REQUISE : rotation de -X degrés sur le bone Hips")
elif abs(abs(ey) - 90) < 1:
    print(f"=> Différence détectée sur l'axe Y : {ey:.1f}°")
elif abs(abs(ez) - 90) < 1:
    print(f"=> Différence détectée sur l'axe Z : {ez:.1f}°")
else:
    print("=> Pas de rotation standard détectée — à analyser manuellement.")

print("="*60)
print("Copie ce résultat complet et donne-le à Claude pour la correction.")
print("="*60 + "\n")
