---
name: art-postprocess
description: Post-process game art for this MetroidVania project — pixelize HD/AI images with PixelOE or codixel, lock a shared palette across frames, clean silhouettes and outlines, recolor variants, pack fixed-cell sprite sheets, and import into Unity 6 URP 2D with pixel-perfect settings. Use for 픽셀화, 후처리, 팔레트, 색 변형, 스프라이트 시트 패킹, Unity 임포트.
---

# Post-processing

The checkpoint list P1–P12 and Unity settings are in `docs/art-pipeline/postprocess.md` — read it first and report which checkpoints were done.

## Pixelize (P3)
- **PixelOE** (best for one HD/AI image; keeps eyes and edges):
  `Tools/ArtSkills/.venv/Scripts/pixeloe.pixelize.exe in.png -O out.png --mode contrast --target_size <px> --patch_size <n> --thickness 2 --colors <k>`
  Needs `setup.ps1 -WithPixelOE` (installs torch). If missing, say so or use codixel.
- **codixel** (frames / no torch): `P.resize` → `P.quantize(buf, n, { dither: true })` → `P.cleanup({ tol: 20 })` → `P.smoothEdge`. Multi-frame: quantize frame 1 at full size, reuse its palette for every frame, then `P.resize({ mode: true })`.
- **ASF video sets**: `finish_frames.py` pixel finish (shared palette, selective outline, binary alpha, `--canvas WxH`).

## Palette & variants (P4)
- Shared project palette (once decided, `docs/art-pipeline/README.md` §4): codixel `P.quantize(buf, n, { palette })`, pixelfx `palette` option, ASF `palette_tool.py`, `colour_lock.py`.
- Enemy/skin variants without regenerating: `sprite-gen recolor-palette --base sheet.png --out palette.draft.json` → edit → `sprite-gen recolor --run-dir <run> --spec recolor.spec.json`.

## Silhouette & outline (P5)
`P.edgeCheck` → `P.smoothEdge`; outline last: `P.selout(P.rimLight(buf, ramp), lit, shade)` with colors from the sprite's own ramp.

## Pack (P10–P11)
Same cell size for every frame of a clip set (max bounds incl. weapon reach), pivot bottom-center, `P.pack(frames, { cols })`. Write `<clip>.json` next to the PNG: cellW, cellH, cols, count, msPerFrame, loop, hitFrame, pivot.

## Unity import (P12)
1. Copy approved PNG + JSON to `Assets/Art/<Characters|FX|Tiles>/<asset>/`.
2. Import settings: Sprite (2D and UI), Multiple, PPU = project value, **Point**, **Compression None**, mipmaps off, Clamp; Sprite Editor → Slice → Grid By Cell Size, pivot Bottom Center.
3. Build AnimationClips at the JSON's ms/frame; hit frames → AnimationEvent; pixelfx `stop` → hit-stop in code.
4. Pixel Perfect Camera with the same PPU.
The upstream JSON formats are not read natively by Unity; no importer script exists yet — do it by hand or propose writing one.
