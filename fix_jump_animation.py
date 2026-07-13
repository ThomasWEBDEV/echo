"""
Script Python pur — Correction animation Jump dans le GLB
Exécuter depuis WSL ou tout terminal Python 3 :
    python3 fix_jump_animation.py

Ne nécessite pas Blender.
Modifie directement le JSON embarqué dans le fichier GLB binaire.
"""

import struct
import json
import shutil
from pathlib import Path

GLB_SOURCE = Path(r"/mnt/c/Users/thoma/Desktop/echo/raw_assets/character/ybot.glb")
GLB_GODOT  = Path(r"/mnt/c/Users/thoma/Desktop/echo/godot_project/assets/characters/ybot.glb")
GLB_BACKUP = GLB_SOURCE.with_suffix(".glb.bak")

# ── 1. Sauvegarde préventive ──────────────────────────────────────────────────
shutil.copy2(GLB_SOURCE, GLB_BACKUP)
print(f"[ECHO] Sauvegarde → {GLB_BACKUP}")

# ── 2. Lire le GLB ───────────────────────────────────────────────────────────
raw = GLB_SOURCE.read_bytes()
magic, version, total_len = struct.unpack_from("<III", raw, 0)
assert magic == 0x46546C67, "Pas un fichier GLB valide"

json_chunk_len, json_chunk_type = struct.unpack_from("<II", raw, 12)
assert json_chunk_type == 0x4E4F534A, "Premier chunk attendu = JSON"

json_start = 20
json_bytes = raw[json_start : json_start + json_chunk_len]
gltf = json.loads(json_bytes)

bin_header_offset = json_start + json_chunk_len
bin_chunk_len, bin_chunk_type = struct.unpack_from("<II", raw, bin_header_offset)
bin_data = raw[bin_header_offset + 8 : bin_header_offset + 8 + bin_chunk_len]

print(f"[ECHO] GLB lu : {total_len} octets, {len(gltf.get('animations', []))} animations")

# ── 3. Lister les animations avant modification ───────────────────────────────
print("[ECHO] Animations avant correction :")
for a in gltf.get("animations", []):
    print(f"  - '{a['name']}'")

# ── 4. Supprimer l'ancienne "Jump" et renommer "Jump.001" → "Jump" ────────────
animations = gltf.get("animations", [])
animations_new = []
renamed = False
removed = False

for anim in animations:
    if anim["name"] == "Jump":
        print(f"[ECHO] Animation 'Jump' supprimée (root motion, {len(anim['channels'])} channels).")
        removed = True
        # on la garde pas
    elif anim["name"] == "Jump.001":
        anim["name"] = "Jump"
        animations_new.append(anim)
        print(f"[ECHO] Animation 'Jump.001' renommée en 'Jump' ({len(anim['channels'])} channels).")
        renamed = True
    else:
        animations_new.append(anim)

if not removed:
    print("[WARN] Animation 'Jump' introuvable — rien à supprimer.")
if not renamed:
    print("[WARN] Animation 'Jump.001' introuvable — rien à renommer.")

gltf["animations"] = animations_new

# ── 5. Lister les animations après modification ───────────────────────────────
print("[ECHO] Animations après correction :")
for a in gltf["animations"]:
    print(f"  - '{a['name']}'")

# ── 6. Réécrire le JSON chunk (aligné sur 4 octets) ──────────────────────────
new_json_bytes = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
# Padding espace jusqu'au prochain multiple de 4
remainder = len(new_json_bytes) % 4
if remainder:
    new_json_bytes += b" " * (4 - remainder)

# ── 7. Reconstruire le GLB complet ───────────────────────────────────────────
new_json_chunk_len = len(new_json_bytes)
new_total_len = 12 + 8 + new_json_chunk_len + 8 + bin_chunk_len

glb_out = bytearray()
# Header
glb_out += struct.pack("<III", 0x46546C67, 2, new_total_len)
# JSON chunk
glb_out += struct.pack("<II", new_json_chunk_len, 0x4E4F534A)
glb_out += new_json_bytes
# BIN chunk
glb_out += struct.pack("<II", bin_chunk_len, 0x004E4942)
glb_out += bin_data

# ── 8. Écrire les deux destinations ──────────────────────────────────────────
GLB_SOURCE.write_bytes(glb_out)
print(f"[ECHO] GLB écrit → {GLB_SOURCE}  ({len(glb_out)} octets)")

shutil.copy2(GLB_SOURCE, GLB_GODOT)
print(f"[ECHO] GLB copié → {GLB_GODOT}")

# ── 9. Vérification finale ────────────────────────────────────────────────────
verify = json.loads(glb_out[20 : 20 + new_json_chunk_len])
all_anim_names = [a["name"] for a in verify["animations"]]
print(f"[ECHO] Vérification — toutes les animations : {all_anim_names}")
assert "Jump" in all_anim_names, "Animation 'Jump' absente du fichier final"
assert "Jump.001" not in all_anim_names, "Animation 'Jump.001' toujours présente — rename échoué"
print("[ECHO] Succès. Reimporter ybot.glb dans Godot (clic droit > Reimport).")
