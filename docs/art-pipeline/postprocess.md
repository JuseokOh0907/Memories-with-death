# 후처리 지점 (Post-processing checkpoints)

> 생성 도구가 끝낸 뒤 **사람/에이전트가 반드시 손대거나 확인해야 하는 지점**. 숫자 QA 는 해부·정체성·모션 품질을 승인하지 않는다(ASF·2dimg2motion·codixel 공통 원칙) — 매 단계 컨택트 시트/GIF 를 눈으로 본다.
> 카탈로그는 [`README.md`](README.md).

## 체크포인트 한눈에

| # | 지점 | 언제 | 도구 | 통과 기준 |
|---|---|---|---|---|
| P1 | 배경 제거·알파 정리 | 원화·AI 출력 직후 | sprite-gen `cutout --white-check`, ASF 소프트 매트, codixel `P.removeBg({erode})` | 흰 배경 위에서 크로마/보라 테두리 없음 |
| P2 | 기준 이미지 정규화 | 애니메이션 전 | 2dimg2motion `standardize_baseline.py`, `P.trim` | 공격·점프 여유 여백, 발 기준선 고정 |
| P3 | 픽셀화 | HD/AI 이미지 → 픽셀 | PixelOE, codixel `resize→quantize→cleanup→smoothEdge`, ASF `finish_frames.py pixel` | 눈 등 작은 디테일이 회색 점으로 뭉개지지 않음 |
| P4 | 팔레트 락 | 픽셀화 직후, 전 프레임 | codixel `P.quantize({palette})`, ASF `palette_tool.py`/`colour_lock.py` | 프레임 간 색 깜빡임 없음, 지정 색수 이하 |
| P5 | 실루엣·외곽선 | 프레임 확정 전 | `P.smoothEdge`, `P.edgeCheck`, `P.rimLight`→`P.selout` | 1px 돌기/구멍 없음. **외곽선은 리깅 후 최종 프레임에** |
| P6 | 리그 소유 검증 | 리깅 직후, 애니 전 | `rig.ownerMap()`+`legend()`, `P.ruler` | 어깨가 머리 색으로 칠해지는 등 잘못된 소유 없음 |
| P7 | 모션 검수 | 애니메이션 후 | 번호 붙은 프레임 시트, `P.gif`, Pixelbolt `inspect --view onion`, sprite-gen `inspect-motion` | 발 미끄러짐·손 바뀜·스케일 튐·루프 이음새 없음 |
| P8 | 루프/원샷 타이밍 | 모션 확정 시 | ASF `gait_loop.py`/`retime.py`, pixelfx `act` | 루프 시작=끝, 원샷 히트 프레임 고정 |
| P9 | 이펙트 합성 검수 | 이펙트를 캐릭터에 붙일 때 | pixelfx `tools/act.mjs <id>`, `check.mjs` | 와인드업→정지(히트스톱)→회복이 캐릭터 포즈와 일치 |
| P10 | 고정 캔버스·피벗 | 패킹 직전 | `P.pack`, ASF `--canvas 48x64`, Pixelbolt `sheet` | 모든 프레임 같은 셀 크기, 피벗 = 발 중앙 |
| P11 | 시트 패킹·메타 | 출력 | `P.pack`, pixelfx `render.mjs --sheet`, Pixelbolt `render`, ASF `export_engine.py --target aseprite-json` | 셀 크기·프레임 수·열 수·ms 기록 |
| P12 | Unity 임포트 | `Assets/Art/` 로 옮긴 뒤 | Unity 설정 (아래) | 흐림·번짐·색 압축 없음, 피벗 정렬 |

## 상세

### P1 배경 제거·알파
- AI 출력은 마젠타/그린 크로마 위에서 받는다. 흰 배경은 `cutout --white-check` 로 매트 처리.
- 마젠타 키는 연보라색 의상을 같이 지운다 → `--protect-color` 또는 그린 키 (ASF known-limitations).
- 픽셀 아트 최종본은 **이진 알파**(0/255). 반투명 가장자리는 P3 에서 정리.

### P2 기준 이미지 정규화
- 2dimg2motion: `python Tools/ArtSkills/vendor/2dimg2motion/scripts/standardize_baseline.py in.png --subject-max 360 --margin-ratio 0.75`
- 한 배치에는 **스케일 전략 하나**만. 프레임마다 자동 맞춤하면 스케일이 튄다.

### P3 픽셀화 — 경로 선택
| 입력 | 권장 |
|---|---|
| 고해상도 일러스트/AI 이미지 1장 | **PixelOE** `--mode contrast --thickness 2 --colors <팔레트 수>` → 이후 codixel `cleanup/smoothEdge` |
| 여러 프레임(애니메이션) | 1프레임을 풀사이즈에서 quantize → 그 팔레트를 나머지 프레임에 적용 → `P.resize({mode:true})` (codixel) |
| ASF 영상 세트 | `finish_frames.py` pixel 마무리 (공유 팔레트, 선택 외곽선, 이진 알파, `--canvas`) |
| sprite-gen 아틀라스 | 백본 래티스(pixel-unfake)로 그리드 고정 |
- 디더는 **두 색 경계 띠에만** (codixel `P.band`). 전면 디더 금지.
- 램프는 색조 이동(`P.hueRamp`): 어두운 쪽 → 청/보라, 밝은 쪽 → 노랑/주황.

### P5·P6 리깅 관련 순서 규칙 (codixel)
- 이미지 1장: **픽셀화 → 뼈대**. 여러 이미지 합성: **뼈대(풀사이즈) → 프레임별 픽셀화**.
- 64px 미만 스프라이트나 큰 회전은 픽셀 상태에서 돌리면 계단이 깨진다 → 풀사이즈에서 리깅·회전 후 프레임마다 픽셀화.
- `P.outline` 은 버퍼를 2px 키우고 1px 밀어낸다 → 관절 좌표는 외곽선 전에 읽는다.

### P7·P8 모션
- Claude 는 GIF 움직임을 못 본다 → **번호 매긴 프레임 시트**로 검수, 사용자에게는 GIF 제공.
- 이동 루프(run)는 발 접지 프레임을 확인: sprite-gen `inspect-motion --contacts 0:4 --foot-box ...`.
- 2dimg2motion 결과는 `validate_14frame_pattern.py` 가 `OK` 를 내도 손 바뀜·클리핑·시안 테두리는 눈으로 본다.
- 공격 원샷은 히트 프레임을 데이터로 남긴다 → 게임 코드의 히트박스 활성 타이밍과 맞춘다.

### P9 이펙트 ↔ 캐릭터 동기화 (pixelfx act)
- `fx.act.windup` 만큼 **이펙트보다 먼저** 캐릭터 와인드업 시작, `hits[].t` 에 히트박스, `hits[].stop` 초만큼 히트스톱(Time.timeScale 또는 애니메이터 정지), `shake/amp` 는 카메라 셰이크.
- 시트 JSON 의 `meta.pixelfx.act` 를 Unity 에서 읽어 AnimationEvent 로 옮기는 것을 권장.
- pixelfx 의 몸에 그려지는 이펙트(frost, poison)는 기본 더미 기준 → 우리 캐릭터로 쓰려면 재생성 필요.
- light 버퍼(가산 발광)는 별도 레이어로 뽑아 **Additive 블렌딩** 스프라이트로 겹친다.

### P10~P11 패킹
- 셀 크기는 액션 전체에서 최대 바운드 기준으로 통일 (공격 칼끝·점프 높이 포함). 피벗은 발 중앙(bottom-center).
- 프레임 메타(셀 w/h, 열, 프레임 수, ms)를 시트 옆 `*.json` 으로 남긴다.

### P12 Unity 임포트 (Unity 6 / URP 2D)
| 설정 | 값 |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Multiple → Sprite Editor → Slice: **Grid By Cell Size** (P10 셀 크기) |
| Pixels Per Unit | 프로젝트 공통값 (README §4 열린 결정) |
| Filter Mode | **Point (no filter)** |
| Compression | **None** |
| Generate Mip Maps | Off |
| Pivot | Bottom Center (또는 Custom = 발 위치) |
| Wrap Mode | Clamp |
- 카메라: URP **Pixel Perfect Camera** (Assets PPU 일치, Reference Resolution 설정).
- **자동화 (2026-11-06):** `Assets/Art/` 아래 PNG 는 `PixelArtTexturePostprocessor` 가 위 설정으로 임포트한다. Pixelbolt 아틀라스는 `Tools/ArtSkills/scripts/atlas_to_unity.py` 로 `<png>.tiles.json` 을 만들어 옆에 두면 그대로 잘리고 Physics Shape 가 들어가며, 메뉴 **Tools ▸ Art ▸ Build Tiles From Manifests** 가 Rule Tile(blob 47)·Tile 에셋을 만든다. 붙음 면 세트(`--flush-of`)가 있으면 Solid 는 `AttachRuleTile` 이 되고, `--sink` 는 소품을 N px 낮게 그린다(피벗 이동, 충돌 함께). 절차 [`guides/cold-hell-tiles-setup.md`](../guides/cold-hell-tiles-setup.md).
- 애니메이션 JSON(Aseprite JSON array, pixelfx meta) → AnimationClip·AnimationEvent 임포터는 아직 없다. 캐릭터 애니메이션이 나올 때 만든다.
- Pixelbolt `pixelforge-normals` 로 노멀 패스를 만들면 URP 2D Light 의 Secondary Texture(`_NormalMap`)로 연결 가능.
- 최종본만 `Assets/Art/...` 로. 후보·실패 테이크는 `Tools/ArtSkills/_work/` 에 둔다(2dimg2motion 원칙: 최종 폴더는 납품물만).
