"""
Script Blender — Nettoyage des animations parasites dans le GLB
Blender 5.2 — Scripting > Open > cleanup_animations_blender.py > Run Script

Ce script :
  1. Importe le GLB actuel (23 animations dont 4 parasites)
  2. Supprime : Action.001, Action.002,
                Armature.001|mixamo.com|Layer0, Armature.001|mixamo.com|Layer0.001
  3. Supprime l'ancien "RunningJump" (Mixamo original, sans fix de direction)
  4. Renomme "RunningJump.001" → "RunningJump" (notre version retargetée, direction correcte)
  5. Exporte le GLB final (19 animations propres)
  6. Copie dans godot_project

Résultat attendu : 19 actions dans la console.
"""

import bpy
import shutil

GLB_SOURCE = r"C:\Users\thoma\Desktop\echo\raw_assets\character\ybot.glb"
GLB_GODOT  = r"C:\Users\thoma\Desktop\echo\godot_project\assets\characters\ybot.glb"

# Actions parasites à supprimer complètement
ACTIONS_TO_DELETE = {
    "Action.001",
    "Action.002",
    "Armature.001|mixamo.com|Layer0",
    "Armature.001|mixamo.com|Layer0.001",
    "RunningJump",   # Mixamo original sans fix de direction — remplacé par RunningJump.001
}

# ─── 1. Vider la scène ───────────────────────────────────────────────────────
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for col in [bpy.data.meshes, bpy.data.armatures, bpy.data.actions,
            bpy.data.materials, bpy.data.cameras, bpy.data.lights]:
    for item in list(col):
        col.remove(item, do_unlink=True)
print("[ECHO] Scène vidée.")

# ─── 2. Importer le GLB ──────────────────────────────────────────────────────
bpy.ops.import_scene.gltf(filepath=GLB_SOURCE)
glb_arm = next((o for o in bpy.data.objects if o.type == 'ARMATURE'), None)
assert glb_arm, "[ERREUR] Armature GLB non trouvée."
print(f"[ECHO] Armature GLB : '{glb_arm.name}' — {len(bpy.data.actions)} actions chargées.")

# ─── 3. Supprimer les NLA strips/tracks des actions parasites ─────────────────
# Sauvegarder le nom AVANT suppression (la référence RNA est invalidée après remove)
if glb_arm.animation_data:
    for track in list(glb_arm.animation_data.nla_tracks):
        strips_to_remove = [
            (strip, strip.action.name)
            for strip in track.strips
            if strip.action and strip.action.name in ACTIONS_TO_DELETE
        ]
        for strip, strip_action_name in strips_to_remove:
            track.strips.remove(strip)
            print(f"[ECHO] Strip supprimé (action : '{strip_action_name}').")
        if not track.strips:
            track_name = track.name
            glb_arm.animation_data.nla_tracks.remove(track)
            print(f"[ECHO] Track vide supprimée : '{track_name}'.")

# ─── 4. Supprimer les actions parasites ──────────────────────────────────────
for name in ACTIONS_TO_DELETE:
    action = bpy.data.actions.get(name)
    if action:
        bpy.data.actions.remove(action, do_unlink=True)
        print(f"[ECHO] Action supprimée : '{name}'")
    else:
        print(f"[WARN] Action '{name}' non trouvée (déjà absente).")

# ─── 5. Renommer "RunningJump.001" → "RunningJump" ───────────────────────────
rj001 = bpy.data.actions.get("RunningJump.001")
assert rj001, "[ERREUR] Action 'RunningJump.001' introuvable — vérifier le GLB source."
rj001.name = "RunningJump"
print("[ECHO] 'RunningJump.001' renommée en 'RunningJump'.")

# Renommer aussi dans les NLA tracks
if glb_arm.animation_data:
    for track in glb_arm.animation_data.nla_tracks:
        if track.name in ("RunningJump.001", "RunningJump"):
            track.name = "RunningJump"
        for strip in track.strips:
            if strip.action and strip.action.name == "RunningJump":
                strip.name = "RunningJump"

# ─── 6. Vérification ─────────────────────────────────────────────────────────
print("\n[ECHO] Actions finales :")
for a in sorted(bpy.data.actions, key=lambda x: x.name):
    print(f"  - '{a.name}'")
total = len(bpy.data.actions)
print(f"[ECHO] Total : {total} actions (attendu : 19)")

assert bpy.data.actions.get("Jump"),        "[ERREUR] 'Jump' absent."
assert bpy.data.actions.get("RunningJump"), "[ERREUR] 'RunningJump' absent."
assert not bpy.data.actions.get("RunningJump.001"), "[ERREUR] 'RunningJump.001' encore présent."
assert not bpy.data.actions.get("Action.001"),      "[ERREUR] 'Action.001' encore présent."

# ─── 7. Exporter en GLB ──────────────────────────────────────────────────────
bpy.ops.export_scene.gltf(
    filepath=GLB_SOURCE,
    export_format='GLB',
    export_animations=True,
    export_anim_single_armature=True,
    export_current_frame=False,
    export_rest_position_armature=False,
)
print(f"[ECHO] GLB exporté → {GLB_SOURCE}")

# ─── 8. Copier vers godot_project ────────────────────────────────────────────
shutil.copy2(GLB_SOURCE, GLB_GODOT)
print(f"[ECHO] GLB copié   → {GLB_GODOT}")
print("[ECHO] Terminé. Dans Godot : clic droit sur ybot.glb > Reimport.")
