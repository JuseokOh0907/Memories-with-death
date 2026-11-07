# 005 · 아트 스킬 파이프라인 구성 (2026-11-06)

## 결정
- 외부 아트 도구 7종(agent-sprite-forge, sprite-gen, 2dimg2motion, codixel, Pixelbolt, pixelfx, PixelOE)을 `Tools/ArtSkills/vendor/` 에 **저장소 구조 그대로** 둔다. `Assets/` 밖이라 Unity 가 임포트하지 않는다. git 에는 넣지 않고 `setup.ps1` 이 고정 커밋으로 복원한다 (약 270MB).
- upstream 스킬을 `.claude/skills/` 에 직접 넣지 않고, 기능별 진입 스킬 6종(`art-pipeline`, `art-master`, `art-rig`, `art-animate`, `art-fx`, `art-postprocess`)이 필요할 때 upstream SKILL.md 를 원래 위치에서 읽게 한다.
- 기본 스타일: 픽셀 아트, 측면 오른쪽 보기, Unity 6 URP 2D.

## 이유
- codixel·Pixelbolt·pixelfx 가 모두 "픽셀 아트 요청"에 발동하도록 쓰여 있어 그대로 두면 서로 충돌한다. 라우터가 기능별로 하나를 고른다.
- ASF(스킬 5종이 서로의 스크립트를 상대 경로로 호출), pixelfx·Pixelbolt(스킬 폴더 두 단계 위의 저장소 루트 사용)는 저장소 구조가 깨지면 동작하지 않는다.
- 이미지·영상 모델 경로가 없는 지금은 코드 기반 경로(codixel 리깅, Pixelbolt 템플릿, pixelfx 이펙트, PixelOE 픽셀화)가 기본이 된다. 키가 생기면 ASF `video2dsprite` 경로가 열린다.

## 열린 항목
셀 크기, PPU, 공통 팔레트, AI 생성 경로 사용 여부 — `docs/art-pipeline/README.md` §4.
