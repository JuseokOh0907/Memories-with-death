"""Convert a Pixelbolt TexturePacker-style atlas.json into the tile manifest Unity reads.

Usage:
  Tools/ArtSkills/.venv/Scripts/python.exe Tools/ArtSkills/scripts/atlas_to_unity.py <atlas.json> <out.tiles.json> \
      [--autotile blob] [--collider sprite|none] [--frame-pattern "frost-{mask}-{variant}"]
      [--flush-of ColdHell_FrostStone.png --flush-side n|s|ns] [--sink spikes-floor=4 ...]

Output (read by Assets/Editor/ArtImport/*.cs, one file next to its PNG as <png-name>.tiles.json):
  rects in texture pixels with a bottom-left origin (Unity), collision polygons in pixels relative to
  the sprite rect centre with y up (ISpritePhysicsOutlineDataProvider), plus mask/variant/weight for Rule Tiles.
"""
import argparse
import json
import re

ap = argparse.ArgumentParser()
ap.add_argument("atlas")
ap.add_argument("out")
ap.add_argument("--autotile", default="", help="blob for a 47-tile blob set, empty for plain tiles")
ap.add_argument("--collider", default="sprite", choices=["sprite", "none"])
ap.add_argument("--frame-pattern", default="", help='e.g. "frost-{mask}-{variant}"')
ap.add_argument("--ppu", type=int, default=32)
ap.add_argument("--flush-of", default="", help="blob PNG this set replaces faces of, e.g. ColdHell_FrostStone.png")
ap.add_argument("--flush-side", default="", choices=["", "n", "s", "ns"], help="face(s) the flush set makes plain")
ap.add_argument("--sink", action="append", default=[], metavar="NAME=PX",
                help="draw this tile PX pixels lower (pivot moves up; collision moves with it). Repeatable")
a = ap.parse_args()
sinks = {k: int(v) for k, v in (s.split("=", 1) for s in a.sink)}

atlas = json.load(open(a.atlas, encoding="utf-8"))
tex_h = atlas["meta"]["size"]["h"]

pat = None
if a.frame_pattern:
    rx = re.escape(a.frame_pattern).replace(r"\{mask\}", r"(?P<mask>\d+)").replace(r"\{variant\}", r"(?P<variant>[A-Za-z0-9_]+)")
    pat = re.compile("^" + rx + "$")

tiles = []
for name, f in atlas["frames"].items():
    r = f["frame"]
    x, y, w, h = r["x"], r["y"], r["w"], r["h"]
    t = {"name": name, "x": x, "y": tex_h - y - h, "w": w, "h": h, "mask": -1, "variant": "", "weight": 1, "collision": []}
    tile = f.get("tile") or {}
    t["weight"] = tile.get("weight", 1)
    t["sink"] = sinks.get(name, 0)
    for poly in tile.get("collision", []) or []:
        pts = [{"x": p["x"] - w / 2, "y": (h - p["y"]) - h / 2} for p in poly]
        t["collision"].append({"points": pts})
    if pat:
        m = pat.match(name)
        if m:
            t["mask"] = int(m.group("mask"))
            t["variant"] = m.groupdict().get("variant") or ""
    tiles.append(t)

out = {"ppu": a.ppu, "autotile": a.autotile, "collider": a.collider,
       "flushOf": a.flush_of, "flushSide": a.flush_side, "tiles": tiles}
json.dump(out, open(a.out, "w", encoding="utf-8"), indent=1)
print(f"{a.out}: {len(tiles)} tiles")
