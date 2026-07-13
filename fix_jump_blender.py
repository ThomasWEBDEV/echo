"""
Script Blender (interface graphique) — Remplacement animation Jump
Blender 5.1.2 — Scripting > Open > fix_jump_blender.py > Run Script

Étapes :
  1. Vide la scène manuellement (sans read_homefile qui coupe le script)
  2. Importe raw_assets/character/ybot.glb
  3. Supprime l'action "Jump" existante et sa NLA strip
  4. Importe Downloads/Jump.fbx
  5. Extrait l'action du FBX, la renomme "Jump", la pousse en NLA sur l'armature GLB
  6. Supprime les objets FBX temporaires
  7. Exporte le GLB final, copie dans godot_project
"""

import bpy
import shutil

GLB_SOURCE = r"C:\Users\thoma\Desktop\echo\raw_assets\character\ybot.glb"
GLB_GODOT  = r"C:\Users\thoma\Desktop\echo\godot_project\assets\characters\ybot.glb"
FBX_JUMP   = r"C:\Users\thoma\Downloads\Jump.fbx"

# ─── 1. Vider la scène manuellement ─────────────────────────────────────────
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for col in [bpy.data.meshes, bpy.data.armatures, bpy.data.actions,
            bpy.data.materials, bpy.data.cameras, bpy.data.lights]:
    for item in list(col):
        col.remove(item, do_unlink=True)
print("[ECHO] Scène vidée.")

# ─── 2. Importer le GLB ──────────────────────────────────────────────────────
bpy.ops.import_scene.gltf(filepath=GLB_SOURCE)
print(f"[ECHO] GLB importé : {GLB_SOURCE}")

# Identifier l'armature GLB
glb_arm = next((o for o in bpy.data.objects if o.type == 'ARMATURE'), None)
assert glb_arm, "[ERREUR] Aucune armature trouvée après import GLB."
print(f"[ECHO] Armature GLB : '{glb_arm.name}'")

print("[ECHO] Actions GLB :")
for a in bpy.data.actions:
    print(f"  - '{a.name}'")

# ─── 3. Supprimer action "Jump" et sa NLA strip ──────────────────────────────
if glb_arm.animation_data:
    for track in list(glb_arm.animation_data.nla_tracks):
        for strip in list(track.strips):
            if strip.action and strip.action.name == "Jump":
                track.strips.remove(strip)
                print("[ECHO] NLA strip 'Jump' retirée.")
        if not track.strips:
            glb_arm.animation_data.nla_tracks.remove(track)

old_jump = bpy.data.actions.get("Jump")
if old_jump:
    bpy.data.actions.remove(old_jump, do_unlink=True)
    print("[ECHO] Action 'Jump' supprimée.")
else:
    print("[WARN] Action 'Jump' introuvable.")

# Noms des actions GLB existantes (pour repérer la nouvelle du FBX ensuite)
glb_action_names = {a.name for a in bpy.data.actions}
print(f"[ECHO] {len(glb_action_names)} actions GLB conservées.")

# ─── 4. Importer le FBX ──────────────────────────────────────────────────────
bpy.ops.import_scene.fbx(filepath=FBX_JUMP, automatic_bone_orientation=False)
print(f"[ECHO] FBX importé : {FBX_JUMP}")

# ─── 5. Identifier l'action créée par le FBX ─────────────────────────────────
fbx_action = next((a for a in bpy.data.actions if a.name not in glb_action_names), None)
assert fbx_action, "[ERREUR] Aucune nouvelle action trouvée après import FBX."
print(f"[ECHO] Action FBX trouvée : '{fbx_action.name}' ({len(fbx_action.fcurves)} fcurves)")
fbx_action.name = "Jump"
print("[ECHO] Action renommée en 'Jump'.")

# ─── 6. Pousser l'action "Jump" en NLA strip sur l'armature GLB ──────────────
# Dé-sélectionner tout, activer l'armature GLB
bpy.ops.object.select_all(action='DESELECT')
glb_arm.select_set(True)
bpy.context.view_layer.objects.active = glb_arm

# Assigner l'action à l'armature GLB pour pouvoir la pousser en NLA
if not glb_arm.animation_data:
    glb_arm.animation_data_create()
glb_arm.animation_data.action = bpy.data.actions["Jump"]

# Pousser en NLA (crée une nouvelle track + strip)
bpy.ops.nla.action_pushdown(channel_index=-1)
glb_arm.animation_data.action = None  # libérer l'action active
print("[ECHO] Action 'Jump' poussée en NLA strip sur l'armature GLB.")

# ─── 7. Supprimer les objets importés du FBX (armature + mesh FBX) ────────────
fbx_arm = next((o for o in bpy.data.objects
                if o.type == 'ARMATURE' and o.name != glb_arm.name), None)

fbx_objects = []
if fbx_arm:
    # Trouver les meshes liés à l'armature FBX (modifier Armature)
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
    print(f"[ECHO] {len(fbx_objects)} objet(s) FBX temporaire(s) supprimé(s).")
else:
    print("[WARN] Aucun objet FBX à supprimer (ou FBX sans armature séparée).")

# ─── 8. Vérification avant export ────────────────────────────────────────────
print("[ECHO] Actions finales dans la scène :")
for a in sorted(bpy.data.actions, key=lambda x: x.name):
    print(f"  - '{a.name}'")

jump_check = bpy.data.actions.get("Jump")
assert jump_check, "[ERREUR] Action 'Jump' absente avant export — annulation."

# ─── 9. Exporter en GLB ───────────────────────────────────────────────────────
bpy.ops.export_scene.gltf(
    filepath=GLB_SOURCE,
    export_format='GLB',
    export_animations=True,
    export_anim_single_armature=True,
    export_current_frame=False,
    export_rest_position_armature=False,
)
print(f"[ECHO] GLB exporté → {GLB_SOURCE}")

# ─── 10. Copier vers godot_project ────────────────────────────────────────────
shutil.copy2(GLB_SOURCE, GLB_GODOT)
print(f"[ECHO] GLB copié   → {GLB_GODOT}")
print("[ECHO] Terminé. Dans Godot : clic droit sur ybot.glb > Reimport.")
