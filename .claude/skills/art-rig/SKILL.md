---
name: art-rig
description: Rig a pixel-art character, creature or long body (snake, tail, dragon) with bones for this MetroidVania project — codixel P.Rig / P.bend from a reference image, or Pixelbolt pose sources with two-bone IK and motion templates. Use for 리깅, 뼈대, bones, skeleton, joint setup before animating.
---

# Rigging

## Choose
| Case | Tool | Read |
|---|---|---|
| One image of a character/creature → bones → poses (default) | codixel `P.Rig` | `Tools/ArtSkills/vendor/codixel/SKILL.md` → "Bones", section 2 steps 3–4 |
| Long body along a spine (tail, snake, tentacle, dragon) | codixel `P.bend` + `P.wave` | same, "Long bodies" |
| Built from parts in a JSON recipe, need idle/walk/run/jump/attack/hurt templates and planted-feet checks | Pixelbolt `*.poses.json` | `Tools/ArtSkills/vendor/Pixelbolt/skills/pixel-art/SKILL.md`, `vendor/Pixelbolt/docs/art-workflow.md#structural-limbs-and-bodies`, example `vendor/Pixelbolt/examples/swing.poses.json` |
| No reference, code-drawn hero with FK/IK and ground constraint | ASF codeart2d `rig_animate.py` (disclose as code-drawn) | `Tools/ArtSkills/vendor/agent-sprite-forge/skills/codeart2d/SKILL.md` |

## codixel rig procedure
1. `const P = require("<abs path>/Tools/ArtSkills/vendor/codixel/core/pixel.js")`. Scripts go in `Tools/ArtSkills/_work/<asset>/`, never inside `vendor/`.
2. Order: single image → **pixelize first, then bones**. Sprite under ~64 px or large rotations → bones on the clean full-size image, pixelize every frame afterwards with a shared palette.
3. Read joints with `P.ruler(sprite, 6, { step: 8 })` — never guess coordinates. Parents before children: body → head, upper arm → forearm → weapon, thigh → shin.
4. `bind({ joint: 2, paint: {...} })`; verify with `rig.ownerMap()` + `rig.legend()` saved via `P.sheet`. Fix wrong owners with `paint` and show again (checkpoint P6).
5. Legs: `P.ik(hipX, hipY, footX, footY, upper, lower, dir)` to plant feet.
6. Save the rig definition (bones, bind, rest pose) as `rig.js`/`rig.json` in the work folder so `art-animate` reuses it.

Pixelbolt check: `node Tools/ArtSkills/vendor/Pixelbolt/bin/pixelbolt.js inspect char.poses.json --motion run --speed 48`.

Outline (`P.outline`/`P.selout`) only on final frames, after rigging (it shifts coordinates by 1 px). Checkpoints P5, P6 in `docs/art-pipeline/postprocess.md`.
