"""
Script Python pur — Suppression de l'animation "Jump" du GLB
Exécuter depuis WSL ou tout terminal Python 3 :
    python3 remove_jump_animation.py

Ne nécessite pas Blender.
Modifie directement le JSON embarqué dans le fichier GLB binaire.
"""

import struct
import json
import shutil
from pathlib import Path

GLB_SOURCE = Path("/mnt/c/Users/thoma/Desktop/echo/raw_assets/character/ybot.glb")
GLB_GODOT  = Path("/mnt/c/Users/thoma/Desktop/echo/godot_project/assets/characters/ybot.glb")
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
print("[ECHO] Animations avant suppression :")
for a in gltf.get("animations", []):
    print(f"  - '{a['name']}'")

# ── 4. Supprimer uniquement "Jump" ────────────────────────────────────────────
animations = gltf.get("animations", [])
animations_new = [a for a in animations if a["name"] != "Jump"]
removed_count = len(animations) - len(animations_new)

if removed_count == 0:
    print("[WARN] Aucune animation nommée 'Jump' trouvée — rien à supprimer.")
else:
    print(f"[ECHO] {removed_count} animation(s) 'Jump' supprimée(s).")

gltf["animations"] = animations_new

# ── 5. Lister les animations après modification ───────────────────────────────
print("[ECHO] Animations après suppression :")
for a in gltf["animations"]:
    print(f"  - '{a['name']}'")

# ── 6. Vérification : aucune "Jump" ne doit subsister ────────────────────────
remaining_jump = [a for a in gltf["animations"] if a["name"] == "Jump"]
assert len(remaining_jump) == 0, f"ERREUR : {len(remaining_jump)} animation(s) 'Jump' encore présente(s) !"

# ── 7. Réécrire le JSON chunk (aligné sur 4 octets) ──────────────────────────
new_json_bytes = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
remainder = len(new_json_bytes) % 4
if remainder:
    new_json_bytes += b" " * (4 - remainder)

# ── 8. Reconstruire le GLB complet ───────────────────────────────────────────
new_json_chunk_len = len(new_json_bytes)
new_total_len = 12 + 8 + new_json_chunk_len + 8 + bin_chunk_len

glb_out = bytearray()
glb_out += struct.pack("<III", 0x46546C67, 2, new_total_len)
glb_out += struct.pack("<II", new_json_chunk_len, 0x4E4F534A)
glb_out += new_json_bytes
glb_out += struct.pack("<II", bin_chunk_len, 0x004E4942)
glb_out += bin_data

# ── 9. Écrire les deux destinations ──────────────────────────────────────────
GLB_SOURCE.write_bytes(glb_out)
print(f"[ECHO] GLB écrit → {GLB_SOURCE}  ({len(glb_out)} octets)")

shutil.copy2(GLB_SOURCE, GLB_GODOT)
print(f"[ECHO] GLB copié → {GLB_GODOT}")

# ── 10. Vérification finale sur le fichier écrit ─────────────────────────────
verify_raw = glb_out
verify_json_len = struct.unpack_from("<I", verify_raw, 12)[0]
verify = json.loads(verify_raw[20:20 + verify_json_len])
all_names = [a["name"] for a in verify["animations"]]
print(f"[ECHO] Vérification finale — {len(all_names)} animations : {all_names}")
assert "Jump" not in all_names, "ERREUR : 'Jump' toujours présente dans le fichier final !"
print(f"[ECHO] OK — aucune animation 'Jump' dans le GLB final.")
print("[ECHO] Reimporter ybot.glb dans Godot (clic droit > Reimport).")
