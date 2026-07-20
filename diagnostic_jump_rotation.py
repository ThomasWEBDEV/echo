"""
Script Blender — DIAGNOSTIC rotation Jump
Blender 5.2 — Scripting > Open > diagnostic_jump_rotation.py > Run Script

Lit les rotations de l'armature GLB et FBX après import,
et les rotations du bone Hips à frame 0 et frame 1.
N'exporte RIEN. Lecture seule (hormis la scène Blender temporaire).
"""

import bpy
import mathutils

GLB_SOURCE = r"C:\Users\thoma\Desktop\echo\raw_assets\character\ybot.glb"
FBX_JUMP   = r"C:\Users\thoma\Downloads\Jump.fbx"

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
assert glb_arm, "Aucune armature GLB trouvée."

print("\n=== ARMATURE GLB ===")
print(f"  Nom            : {glb_arm.name}")
print(f"  Rotation Euler : {tuple(round(r, 4) for r in glb_arm.rotation_euler)}")
print(f"  Rotation Quat  : {tuple(round(r, 4) for r in glb_arm.rotation_quaternion)}")
print(f"  Scale          : {tuple(round(s, 4) for s in glb_arm.scale)}")

# Bones Hips dans la pose de repos (edit mode)
bpy.context.view_layer.objects.active = glb_arm
bpy.ops.object.mode_set(mode='EDIT')
edit_bones = glb_arm.data.edit_bones
hips_glb = next((b for b in edit_bones if 'Hips' in b.name), None)
if hips_glb:
    print(f"\n  [GLB] Hips edit bone head : {tuple(round(v, 4) for v in hips_glb.head)}")
    print(f"  [GLB] Hips edit bone tail : {tuple(round(v, 4) for v in hips_glb.tail)}")
    print(f"  [GLB] Hips edit bone roll : {round(hips_glb.roll, 4)} rad = {round(hips_glb.roll * 57.296, 1)} deg")
else:
    print("  [GLB] Hips non trouvé dans edit bones")
bpy.ops.object.mode_set(mode='OBJECT')

# ─── 3. Snapshot actions GLB ─────────────────────────────────────────────────
glb_action_names = {a.name for a in bpy.data.actions}

# ─── 4. Importer le FBX Jump ─────────────────────────────────────────────────
bpy.ops.import_scene.fbx(filepath=FBX_JUMP, automatic_bone_orientation=False)
fbx_arm = next((o for o in bpy.data.objects
                if o.type == 'ARMATURE' and o.name != glb_arm.name), None)
assert fbx_arm, "Aucune armature FBX trouvée."

print("\n=== ARMATURE FBX ===")
print(f"  Nom            : {fbx_arm.name}")
print(f"  Rotation Euler : {tuple(round(r, 4) for r in fbx_arm.rotation_euler)}")
print(f"  Rotation Quat  : {tuple(round(r, 4) for r in fbx_arm.rotation_quaternion)}")
print(f"  Scale          : {tuple(round(s, 4) for s in fbx_arm.scale)}")

bpy.context.view_layer.objects.active = fbx_arm
bpy.ops.object.mode_set(mode='EDIT')
edit_bones_fbx = fbx_arm.data.edit_bones
hips_fbx = next((b for b in edit_bones_fbx if 'Hips' in b.name), None)
if hips_fbx:
    print(f"\n  [FBX] Hips edit bone head : {tuple(round(v, 4) for v in hips_fbx.head)}")
    print(f"  [FBX] Hips edit bone tail : {tuple(round(v, 4) for v in hips_fbx.tail)}")
    print(f"  [FBX] Hips edit bone roll : {round(hips_fbx.roll, 4)} rad = {round(hips_fbx.roll * 57.296, 1)} deg")
else:
    print("  [FBX] Hips non trouvé dans edit bones")
bpy.ops.object.mode_set(mode='OBJECT')

# ─── 5. Action Jump — rotation Hips à frame 1 ────────────────────────────────
fbx_action = next((a for a in bpy.data.actions if a.name not in glb_action_names), None)
if fbx_action:
    print(f"\n=== ACTION FBX : '{fbx_action.name}' ===")

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

    fcurves = get_fcurves(fbx_action)
    print(f"  Nombre de FCurves : {len(fcurves)}")

    # Chercher les rotations du bone Hips (frame 1)
    for fc in fcurves:
        if 'Hips' in fc.data_path and 'rotation' in fc.data_path:
            if fc.keyframe_points:
                val_f1 = fc.keyframe_points[0].co[1]
                print(f"  {fc.data_path}[{fc.array_index}] @ frame1 = {round(val_f1, 4)}")

    # Location Hips à frame 1
    for fc in fcurves:
        if 'Hips' in fc.data_path and 'location' in fc.data_path:
            if fc.keyframe_points:
                val_f1 = fc.keyframe_points[0].co[1]
                print(f"  {fc.data_path}[{fc.array_index}] @ frame1 = {round(val_f1, 4)}")
else:
    print("\n[WARN] Aucune action FBX trouvée.")

print("\n=== FIN DIAGNOSTIC ===")
print("Copie ce rapport et partage-le pour identifier la correction à appliquer.")
