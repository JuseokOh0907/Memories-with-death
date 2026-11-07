---
name: art-master
description: Make or prepare the master still (원화, 기준 이미지) of a character, enemy or prop for this MetroidVania project — AI master still through agent-sprite-forge or sprite-gen when a route exists, otherwise clean up and standardize a user-provided PNG. Use before rigging or animating a new character.
---

# Master still / baseline

Paths are relative to the project root; tools are in `Tools/ArtSkills/vendor/`.

## Route A — AI generation (needs an API key or Codex/Grok CLI)
1. Check: `python Tools/ArtSkills/vendor/agent-sprite-forge/skills/generate2dmedia/scripts/forge_doctor.py`. If no route, say so and go to Route B.
2. Read `Tools/ArtSkills/vendor/agent-sprite-forge/skills/generate2dsprite/SKILL.md`, then `master_still.py generate --takes 3` → show takes → `edit` ("THE ONLY CHANGE: …") → `approve`. Output `master.json` (identity recap reused by every motion prompt).
3. Ask for: side view, facing right, pixel finish target. Alternative: `sprite-gen gen --ref ... --facing right` (read `vendor/sprite-gen/SKILL.md`).
4. Set a spend cap (`--budget-usd`, `--max-calls`) on paid routes.

## Route B — user-provided PNG (works offline)
1. Background: plain color → codixel `P.removeBg(buf, { erode: 1 })` + `P.trim`; white/ivory → `sprite-gen cutout img.png --white-check`.
2. Standardize (margin for attack reach and jump height):
   `Tools/ArtSkills/.venv/Scripts/python.exe Tools/ArtSkills/vendor/2dimg2motion/scripts/standardize_baseline.py in.png --subject-max 360 --margin-ratio 0.75 --output Tools/ArtSkills/_work/<asset>/baseline.png`
3. Record an **identity lock** next to it (`identity.md`): face, silhouette, proportions, palette, weapon hand (screen-left/right), accessories. Asymmetric props → note which side; flipping will move them.

## Checkpoints
P1 (alpha clean), P2 (standardized margin, one scale strategy) in `docs/art-pipeline/postprocess.md`. Show the result next to the reference with codixel `P.sheet` and wait for approval before `art-rig` / `art-animate`.
