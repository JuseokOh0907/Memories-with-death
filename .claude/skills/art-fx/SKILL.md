---
name: art-fx
description: Make pixel-art effects for this MetroidVania project — slashes, hits, impacts, dash trails, fire, smoke, explosions, magic, status, pickups, transitions, weather — using pixelfx (244 ready effects with wind-up/hit-stop act timing), Pixelbolt fluid/fx sources, or codixel P.Flame/P.Smoke/P.Flow. Use for 이펙트, VFX, 타격감, game juice, hit spark, spell effect.
---

# Effects

## Choose
| Need | Tool | Read |
|---|---|---|
| Ready effect or attack "move" with timing (default) | **pixelfx** | `Tools/ArtSkills/vendor/pixelfx/skills/pixelfx/SKILL.md`, list: `vendor/pixelfx/docs/CATALOG.md` |
| Physically simulated fire/smoke/explosion/water/slime/sand/electricity | Pixelbolt `pixelforge-fluid` (38 presets `vendor/Pixelbolt/examples/*.fluid.json`) | `vendor/Pixelbolt/skills/pixel-art/SKILL.md` |
| Particles, slashes, shockwave rings, beams, magic circles, 8-direction projectiles | Pixelbolt `pixelforge-fx` (`examples/effects.fx.json`, spell library) | same |
| Realistic flame/smoke column, fire breath, wall of fire, beams | codixel `P.Flame`, `P.Smoke`, `P.Breath`, `P.HellFire`, `P.beams`; extend `P.Flow` for new ones | `vendor/codixel/SKILL.md` §4 |
| Small additive burst (birth/peak/decay/gone) | sprite-gen `pixel-draw --kind effect` | `vendor/sprite-gen/docs/effects.md` |

## pixelfx quick commands (run from `Tools/ArtSkills/vendor/pixelfx`)
- Find: browse `docs/CATALOG.md` or `node tools/list.mjs`.
- Preview: `node tools/render.mjs melee/sword-slash --gif` (contact sheet in `records/`).
- Recolor to project palette: `--opts '{"palette": ["#102","#215","#43a","#8cf","#cff","#fff"]}'` (dark → bright).
- Export for Unity: `node tools/render.mjs <id> --sheet` → transparent PNG grid + JSON with `meta.pixelfx` (fps, loop, columns, pivot, act). Copy outputs to `Tools/ArtSkills/_work/fx/<id>/`, never edit inside `vendor/`.
- New effect: `node tools/new.mjs <category>/<name>` → render → look → fix worst thing → `node tools/check.mjs <id>`. New effects should be authored in a project folder, not committed into the vendor clone.

## Sync with character animation (checkpoint P9)
- Read the effect's `act`: start the character wind-up `windup` seconds before effect t=0, activate the hitbox at `hits[].t`, freeze for `hits[].stop`, camera shake from `shake`/`amp`.
- Body-drawn effects (frost, poison, scan) are authored on the stock dummy — regenerate against our character before shipping.
- pixelfx has a **light** (additive) buffer: export it as a separate layer and use an Additive sprite material in Unity.

Preview GIF first; sprite sheets only after approval. Then `art-postprocess` (palette lock P4, packing P11, Unity P12).
