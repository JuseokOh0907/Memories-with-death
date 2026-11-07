---
name: art-pipeline
description: Router for making game art in this MetroidVania project (pixel art, Unity). Use first for any request to create or change game images — characters, sprites, animation, rigging, effects/VFX, tiles, backgrounds, pixelizing, sprite sheets, Unity sprite import — or 이미지/스프라이트/애니메이션/리깅/이펙트/배경 생성. Picks the right vendored skill (agent-sprite-forge, sprite-gen, 2dimg2motion, codixel, Pixelbolt, pixelfx, PixelOE) and the post-processing checkpoints.
---

# Art pipeline router

Vendored tools live in `Tools/ArtSkills/vendor/<repo>` (outside `Assets/`). If that folder is missing, run `powershell -ExecutionPolicy Bypass -File Tools/ArtSkills/setup.ps1` first.

Full catalog: `docs/art-pipeline/README.md`. Post-processing checkpoints P1–P12: `docs/art-pipeline/postprocess.md`. Read the catalog section you need, not the whole thing.

## Pick by task

| Task | Go to skill | Main upstream file to read |
|---|---|---|
| Character master still / reference cleanup | `art-master` | `vendor/agent-sprite-forge/skills/generate2dsprite/SKILL.md` |
| Bones / rig from one image | `art-rig` | `vendor/codixel/SKILL.md` (Bones section) |
| Main animation set (idle, run, jump, fall, dash, attack, hurt, death) | `art-animate` | depends on route, see that skill |
| Effects / VFX / hit juice | `art-fx` | `vendor/pixelfx/skills/pixelfx/SKILL.md` |
| Pixelize, palette lock, sheet packing, Unity import | `art-postprocess` | `docs/art-pipeline/postprocess.md` |
| Tiles, autotiles, backgrounds, parallax | (here) | `vendor/Pixelbolt/skills/pixel-art/SKILL.md` (autotile, tilemap), codixel Scenes section, `vendor/agent-sprite-forge/skills/generate2dmap/SKILL.md` |

## Rules for every art task

1. **Read the upstream SKILL.md from its real location** inside `Tools/ArtSkills/vendor/...` and follow it. Its relative paths (scripts, `core/`, sibling skills) resolve from that file's folder. Do not copy upstream skills elsewhere.
2. **Check the route before promising AI generation.** On this machine there are no image/video API keys and no `codex`/`grok` CLI (2026-11-06). Code-based tools (codixel, Pixelbolt, pixelfx, PixelOE, ASF codeart2d) work offline. If the task needs an image or video model, run `python Tools/ArtSkills/vendor/agent-sprite-forge/skills/generate2dmedia/scripts/forge_doctor.py` and tell the user what is missing instead of silently falling back.
3. **Complex subjects need a reference.** For characters/creatures, ask for a reference image (codixel rule) unless a master still already exists.
4. **Preview first.** Show a numbered frame sheet + GIF; create game files only after the user approves.
5. **Work files** go to `Tools/ArtSkills/_work/<asset>/`; only approved finals go to `Assets/Art/<category>/<asset>/`.
6. Use the Python venv at `Tools/ArtSkills/.venv` (`Tools/ArtSkills/.venv/Scripts/python.exe`, Python 3.14) — never bare `python` (it resolves to other interpreters on this PC). `PYTHONUTF8=1` is set in `.claude/settings.json`; this PC defaults to cp949 and vendored scripts crash without it. Node tools run with system Node (24). Capability check: `forge_doctor.py --host-tools none` (Claude Code has no built-in image tool).
7. Project defaults: pixel art, side view facing right, Unity 6 URP 2D, Point filter, no compression, pivot bottom-center. Cell size / PPU / palette are open decisions in `docs/art-pipeline/README.md` §4 — ask if not decided yet and record the answer there.
