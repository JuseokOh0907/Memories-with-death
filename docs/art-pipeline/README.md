# 아트 생성 파이프라인 — 스킬 카탈로그

> 게임 이미지(캐릭터·애니메이션·리깅·이펙트·배경)를 만들 때 **어떤 외부 스킬을 꺼내 쓸지** 정리한 문서.
> 후처리 지점은 [`postprocess.md`](postprocess.md). 각 기능은 `.claude/skills/art-*` 스킬로 바로 호출할 수 있다.
> 기준: 픽셀 아트 · Unity 6 (URP 2D) · 횡스크롤 메트로배니아 · 2026-11-06 README/SKILL.md 기준.

## 0. 설치 위치와 상태

| 경로 | 내용 | git |
|---|---|---|
| `Tools/ArtSkills/vendor/<repo>` | 7개 저장소 원본 (스크립트·SKILL.md 포함). `Assets/` 밖이라 Unity 가 임포트하지 않는다 | 무시 (`setup.ps1` 로 복원) |
| `Tools/ArtSkills/setup.ps1` | 고정 커밋으로 clone + Python venv + 의존성 설치 | 커밋 |
| `.claude/skills/art-*` | 기능별 진입 스킬 6종 (아래 표). upstream SKILL.md 를 **필요할 때만** 읽게 하는 라우터 | 커밋 |
| `Tools/ArtSkills/_work/` | 생성 중간물 작업 폴더 (최종본만 `Assets/Art/` 로 옮김) | 무시 |

upstream 스킬을 `.claude/skills` 에 그대로 넣지 않은 이유: codixel·Pixelbolt·pixelfx 가 모두 "픽셀 아트 요청이면 발동"으로 설명돼 있어 서로 충돌하고, ASF·pixelfx·Pixelbolt 는 스킬 폴더 기준 상대 경로로 자기 저장소의 스크립트를 찾기 때문에 저장소 구조를 유지해야 한다.

### 생성 경로 (중요)

| 경로 | 이 PC 상태 (2026-11-06) | 막히는 것 |
|---|---|---|
| 코드 기반 (codixel, Pixelbolt, pixelfx, PixelOE, ASF codeart2d) | ✅ Node 24, Python 3.14 venv, ffmpeg 8.1 | 없음 — 바로 사용 가능 |

> **Python 은 3.12 이상.** sprite-gen 은 `>=3.11` 로 적혀 있지만 코드가 3.12 문법(PEP 701 f-string)을 써서 3.11 에선 실행되지 않는다. 이 PC 는 PATH 의 `python` 이 3.11(다른 에이전트 venv)로 잡히므로 `setup.ps1` 은 `py -3.14` 로 venv 를 만든다. 스크립트는 항상 `Tools/ArtSkills/.venv/Scripts/python.exe` 로 돌린다.
| 이미지 생성 모델 (ASF master still, sprite-gen gen, 2dimg2motion 키포즈) | ❌ API 키 없음, `codex`/`grok` CLI 없음 | 원화·키포즈 AI 생성 |
| image-to-video (ASF video2dsprite, sprite-gen video-set) | ❌ `XAI_API_KEY`/`FAL_KEY` 등 없음 | 영상 기반 메인 애니메이션 |

- Claude Code 자체에는 이미지 생성 도구가 없다. **2dimg2motion 은 Codex 의 `image_gen` 을 전제**로 하므로 Codex 에서 돌리거나 다른 이미지 모델 경로가 필요하다.
- AI 경로를 열려면: ASF 는 `%APPDATA%\agent-sprite-forge\config.json` 또는 환경 변수(`OPENAI_API_KEY`, `GEMINI_API_KEY`, `XAI_API_KEY`, `FAL_KEY`, `ARK_API_KEY`), sprite-gen 은 `codex`/`grok` 로그인 또는 `XAI_API_KEY`. 키 설정 = 과금 동의로 취급되니 `--budget-usd` 등 상한을 같이 건다.
- 원화를 직접 그리거나 외부 툴로 만든 PNG 를 넣으면 이미지 생성 없이도 **픽셀화·리깅·애니메이션·후처리** 경로는 전부 동작한다.

## 1. 기능별 지도

```
[원화/레퍼런스] ─▶ [픽셀화] ─▶ [리깅] ─▶ [메인 애니메이션] ─▶ [후처리] ─▶ [Unity 임포트]
                                          ▲
                              [이펙트] ───┘ (히트 타이밍 공유)
```

| 기능 | 진입 스킬 | 1순위 도구 | 대안 | 비고 |
|---|---|---|---|---|
| **원화(마스터 스틸)** | `/art-master` | ASF `generate2dsprite` → `master_still.py` | sprite-gen `gen`, 사용자 제공 PNG | AI 경로 필요. 없으면 사용자 PNG |
| **기준 이미지 정규화** | `/art-master` | 2dimg2motion `standardize_baseline.py` | sprite-gen `cutout`, codixel `P.removeBg/trim` | 여백·크기·배경 정리 |
| **픽셀화** | `/art-postprocess` | PixelOE (윤곽 확장 + 대비 다운샘플) | codixel `P.resize/quantize`, ASF `finish_frames.py pixel`, sprite-gen pixel-unfake | AI/HD 이미지 → 픽셀 |
| **리깅(뼈대)** | `/art-rig` | codixel `P.Rig` / `P.bend` | Pixelbolt `*.poses.json`, ASF codeart2d `rig_animate.py`, pixelfx `skeleton()` | 이미지 1장 → 뼈 → 포즈 |
| **메인 애니메이션** | `/art-animate` | (AI 有) ASF `video2dsprite` `sprite_set.py` / (AI 無) codixel 리그 + Pixelbolt 템플릿 | sprite-gen `video-set`, 2dimg2motion 14프레임 | idle·run·jump·fall·dash·attack·hurt·death |
| **이펙트(VFX)** | `/art-fx` | pixelfx (244종 + act 타이밍) | Pixelbolt fluid/fx, codixel `P.Flame/Smoke/Breath`, sprite-gen `pixel-draw --kind effect` | 공격 이펙트는 act 데이터와 히트스톱 공유 |
| **배경·타일·맵** | `/art-pipeline` | Pixelbolt `autotile`·`tilemap`, codixel `P.sky/rockHeight/branchTree` | ASF `generate2dmap`, sprite-gen `background-tile` | Unity Tilemap 직접 출력은 없음 |
| **후처리·엔진 출력** | `/art-postprocess` | 각 도구 export + Unity 설정 | sprite-gen `recolor`, ASF `palette_tool.py` | [`postprocess.md`](postprocess.md) |

## 2. 저장소별 요약

### agent-sprite-forge (ASF) — `vendor/agent-sprite-forge/skills/*` · MIT · Python
원화 1장 → 액션마다 image-to-video 1클립 → 결정적 Python 도구로 키잉·정렬·루프·리타임·마무리·패키징.
- 스킬 5종(폴더 함께 유지): `generate2dsprite`(원화·FX/아이콘 시트·프레임 패키징), `video2dsprite`(액션 세트), `generate2dmap`(맵), `generate2dmedia`(생성 라우터, 키/CLI/비용 장부), `codeart2d`(코드 아트 최후 수단 — 리그·FX·오토타일).
- 핵심 툴: `master_still.py`, `sprite_set.py plan|run|review|accept|report`, `gait_loop.py`(걷기/달리기 루프 측정), `retime.py`(원샷: attack 0.7s, jump 0.9s, hurt 0.5s), `finish_frames.py`(HD 기본 / **pixel**: 공유 팔레트, 선택 외곽선, 이진 알파, `--canvas 48x64`), `colour_lock.py`, `palette_tool.py`, `export_engine.py`(Aseprite JSON, Godot).
- 한계: 에디터 임포트 미검증, 소프트 매트는 마젠타 키 1개 클립 기준 튜닝, 빠른 팔다리·점프는 리테이크 예상.

### sprite-gen — `vendor/sprite-gen` · Apache-2.0 · Python CLI
기본 이미지 1장 → (A) 행 단위 생성 아틀라스 + `manifest.json.frame_layout` / (B) Grok 영상 → 투명 루프.
- 파이프라인 A `prepare → gen-set → extract → compose-atlas → curation`, B `video-set` (`video-canvas → video → video-frames → video-loop`, RIFE 로 루프 보정).
- 유틸: `cutout`, `slice-sheet`, `unpack-atlas` · 후처리: `recolor-palette`, `recolor`(결정적 팔레트 스왑), `export-aseprite` · 에셋: `background-tile`, `shadow`, `inspect-motion`(발 접지·타이밍 측정) · `pixel-draw --kind effect`, `video-face`(말하는 얼굴 층), **Breathe**(아이들 스쿼시&스트레치 굽기), 백본 래티스(픽셀 그리드 고정).
- 좌우 비대칭 소품은 `--handed` + `handed-check`.

### 2dimg2motion — `vendor/2dimg2motion` · MIT · Python(Pillow)
이미지 1장 → **전신 키포즈 재드로잉** 14프레임 시퀀스. 아이덴티티 락 + 토폴로지 락(활성/고정 팔다리)으로 손 바뀜 방지.
- 구조 `00 → 02 → 05 → 08 → 11 → 13` (00/13 = 기준, 02/05/08/11 = 키포즈, 나머지 인비트윈 1/2/2/2/1).
- 하위 스킬: `img2mo-std`(기준 이미지 표준화), `img2mo-learn`(레퍼런스 영상·시트에서 타이밍/프롬프트 지식 축적 → `img2mo-knowledge/`).
- 스크립트는 비창작 후처리만: `standardize_baseline.py`, `fullframes_to_gif.py`, `validate_14frame_pattern.py`.
- 지원 액션: idle, walk, attack, block, hit, death, spawn, cast. **Codex `image_gen` 전제.**

### codixel — `vendor/codixel` · MIT · Node(`core/pixel.js`)
"Claude 는 화가가 아니라 코드를 쓴다" — 레퍼런스 이미지 → 배경 제거 → 픽셀화 → **뼈대** → 이징 애니메이션 → 씬 확인, 단계마다 사용자 확인.
- 리깅: `P.Rig().bone(...).bind({joint, paint})`, `P.ruler`(관절 좌표 읽기), `rig.ownerMap()/legend()`(픽셀 소유 검증), `P.bend`+`P.wave`(뱀·꼬리·용 척추), `P.ik`(2본 다리).
- 픽셀 스타일: `P.hueRamp`(색조 이동 램프), `P.band`(밴드+경계만 디더), `P.selout`/`P.rimLight`, `P.pixelPerfect`, `P.cleanup`, `P.smoothEdge`.
- 이펙트: `P.Flame`, `P.Smoke`, `P.Flow`(확장용), `P.Breath`, `P.HellFire`, `P.novaSmoke`, `P.beams`.
- 출력: `P.gif`(미리보기), `P.pack`(게임용 시트). **미리보기 승인 전엔 게임 파일을 만들지 않는다.**

### Pixelbolt — `vendor/Pixelbolt` · MIT · Node 20+, 의존성 0
JSON 레시피 → 픽셀 스프라이트/타일/아이콘/이펙트/짧은 애니메이션. 결정적, 픽셀 단위 패치 가능.
- **Figures in motion**: 뼈(limbs)·질량(bodies)·그리드(얼굴/소품), 2본 IK, idle/walk/run/jump/attack/hurt 템플릿 → 편집 가능한 포즈. `inspect --motion run` 으로 발 고정·루프 이음새 검사.
- 이펙트: `pixelforge-fluid`(불·연기·폭발·물·꿀·슬라임·모래·전기, 38 프리셋), `pixelforge-fx`(파티클·슬래시·충격파·빔·마법진), `pixelforge-bodies`(강체), wave, `pixelforge-normals`(노멀 패스 → URP 2D 라이트).
- 타일: `autotile`(47 blob / 16 cardinal), `tilemap` 이웃 규칙.
- 출력: PNG 시트 + TexturePacker 스타일 `atlas.json`(피벗·태그), frames/*.png, APNG, GIF, Godot `.tres`, Tiled `.tsx`. 한계: 애니 프레임 256×256 이하.

### pixelfx — `vendor/pixelfx` · MIT · Node 18+
244개 코드 이펙트(moves·fire·explosions·water·ice·lightning·sci-fi·melee·impacts·projectiles·spawn&death·arcane·holy·shadow·status·nature·weather·ui·pickups·transitions·critters).
- **acting**: 61개 + 12 moves 가 `act` 데이터(windup, poses, hits{t, stop(히트스톱), shake, push})를 가진다 → 우리 캐릭터 애니메이션 타이밍에 그대로 씀.
- `palette` 옵션으로 재색(불을 파랗게), `flip/angle/seed`.
- 출력: `node tools/render.mjs <id> --sheet` → 투명 PNG 그리드 + Aseprite 스타일 JSON(`meta.pixelfx`: fps, loop, pivot, act). `tools/new.mjs`·`check.mjs` 로 새 이펙트 제작.
- color 버퍼 + light(가산) 버퍼 2개 → Unity 에선 **2장으로 나눠 Additive 머티리얼**로 쓰는 게 원본에 가깝다.

### PixelOE — `vendor/PixelOE` · Apache-2.0(PyPI `pixeloe`) · Python/PyTorch
AI 없이 고해상도 이미지 → 픽셀 아트. **대비 인지 윤곽 확장** 후 대비 기반 다운샘플 → 눈·외곽선 같은 디테일 보존.
- CLI `pixeloe.pixelize in.png -O out.png --target_size 256 --patch_size 8 --thickness 2 --colors 16 --mode contrast`
- torch API(GPU), Slang 백엔드(CUDA/Vulkan/D3D12/CPU), ComfyUI 노드, Gradio 데모.
- 스킬 없음 → `/art-postprocess` 가 래핑. 이 PC 는 RTX 5070 Ti 라 CUDA 12.8+ 용 torch 필요.

## 3. 메트로배니아 플레이어 기준 액션 세트

`PlayerController` 상태(지상·상승·하강·대시·피격·사망 + 즉시 공격, [decisions/001](../decisions/001-character-structure.md), [002](../decisions/002-attack-and-apex.md))와 맞춘다.

| 클립 | 종류 | 상태 | 권장 경로 |
|---|---|---|---|
| idle | 루프 | 지상(정지) | Pixelbolt idle 템플릿 / sprite-gen Breathe |
| run | 루프 | 지상(이동) | codixel 리그 + `P.ik` / ASF `gait_loop.py` |
| jump_up | 원샷→홀드 | 상승 | 리그 포즈 2~3장 |
| fall | 루프/홀드 | 하강 | 리그 포즈 2장 |
| apex (선택) | 홀드 | 정점(약한 중력) | jump_up 마지막 ↔ fall 첫 프레임 사이 |
| dash | 원샷 | 대시 | 포즈 1~2장 + 잔상은 pixelfx/코드 |
| attack | 원샷 | 공격 | 포즈 + pixelfx melee act 타이밍 (windup/hit/recovery) |
| hurt | 원샷 | 피격 | pixelfx impacts + 리그 포즈 |
| death | 원샷 | 사망 | 리그 / pixelfx spawn&death(dissolve 등) |

## 4. 열린 결정 (정해지면 이 표를 갱신)

| 항목 | 후보 | 영향 |
|---|---|---|
| 캐릭터 셀 크기 | 32×32 / 48×64 / 64×64 | 리깅 방식(64px 미만이면 원본 크기에서 리깅 후 프레임별 픽셀화) |
| **타일 크기 · PPU** | **확정 2026-11-06: 타일 32px, PPU 32** (Grid 1칸 = 1유닛, decisions/004) | 카메라 Pixel Perfect, 콜라이더 크기, 캐릭터는 2칸 안팎(64px급) |
| 팔레트 | 고정 팔레트 1개 공유 여부. 후보: [추운 지옥 23색](palettes/cold-hell.md), [불 지옥 26색](palettes/fire-hell.md) | PixelOE `--colors`, codixel `P.quantize({palette})`, pixelfx `palette` |
| AI 생성 경로 | API 키 / Codex·Grok CLI / 안 씀 | 원화·영상 기반 애니메이션 가능 여부 |
