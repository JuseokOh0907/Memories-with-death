---
name: art-animate
description: Make the main character animation set for this MetroidVania project — idle, run, jump_up, fall, apex, dash, attack, hurt, death — from a master still or a rig. Routes to codixel/Pixelbolt rig animation (offline), agent-sprite-forge video2dsprite or sprite-gen video-set (image-to-video), or 2dimg2motion key-pose redraw. Use for 애니메이션, 메인 애니메이션, sprite sheet of actions, walk/run cycle, attack animation.
---

# Main animation

Clip list and the PlayerController state each one maps to: `docs/art-pipeline/README.md` §3.

## Choose a route
| Route | Needs | Use when | Read |
|---|---|---|---|
| **R1 Rig animation** (default now) | Node only | Rig from `art-rig` exists; precise, editable, consistent identity | `vendor/codixel/SKILL.md` §2 step 4, Pixelbolt motion templates |
| **R2 Image-to-video set** | xAI/fal/BytePlus key or Grok CLI, ffmpeg | Fluid HD-ish motion from one master; expect retakes on run/jump | `vendor/agent-sprite-forge/skills/video2dsprite/SKILL.md` (`sprite_set.py plan/run/review/accept/report`) or `vendor/sprite-gen/SKILL.md` (`video-set`) |
| **R3 Key-pose redraw (14 frames)** | an image model (`image_gen` in Codex) | Short one-shots (attack, hit, death, cast) with identity + topology lock | `vendor/2dimg2motion/SKILL.md` |

Confirm the route with the user if it costs money (R2/R3).

## R1 procedure (codixel / Pixelbolt)
1. Load the rig from `Tools/ArtSkills/_work/<asset>/`. Key poses first, then in-betweens with `P.ease` (never linear).
2. Loops (idle, run, fall): first frame == last frame; run uses contact → down → passing → up. Plant feet with `P.ik`.
3. One-shots: anticipation → action → follow-through → recovery. Attack reads pixelfx `act` timing if an effect is attached (see `art-fx`). Target lengths (ASF retime defaults): attack ≈0.7 s, jump ≈0.9 s, hurt ≈0.5 s.
4. Show a **numbered frame sheet** (`P.sheet`) — you cannot see GIF motion — and give the user `P.gif`. Iterate one fix at a time.
5. After approval: same-size cells, `P.pack`, export per-clip PNG + `clip.json` (cell w/h, cols, frame count, ms per frame, loop, hit frame).

## R2 notes (ASF)
`plan --master master.json` → `run` (resumable, never generates twice) → `review` (look at every sheet) → `accept --action <a> --take <n>` → pixel finish `finish_frames.py` with `--canvas <WxH>` → `export_engine.py --target aseprite-json`. Side views use `--facing right`; flipped left breaks asymmetric props (sprite-gen `--handed`).

## R3 notes (2dimg2motion)
Generate 02/05/08/11 together on one sheet, in-betweens 1/2/2/2/1, 00/13 = baseline. Run `scripts/validate_14frame_pattern.py` then still inspect contact sheet for hand swaps, clipping, fringe, scale popping.

Checkpoints P7, P8, P10, P11 in `docs/art-pipeline/postprocess.md`; then `art-postprocess` for pixel finish and Unity import.
