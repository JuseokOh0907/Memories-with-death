# MetroidVania — Claude 작업 가이드

> Claude Code 가 세션마다 자동으로 읽습니다.
> 1주차 요구사항 원문은 [`docs/week1_mvp_handoff.md`](docs/week1_mvp_handoff.md) — 기능·수치·검증표는 그 문서가 기준입니다.

- 로컬 경로: `C:\Git\MetroidVania` (git 초기화 2026-10-26)
- 저장소: `https://github.com/JuseokOh0907/Memories-with-death` — **로컬 폴더 이름과 저장소 이름이 다르다**
- `C:\Git` 의 형제 프로젝트(Pretty_Knights 등)는 건드리지 않습니다.
- 기획 원본 자료: `C:\GameMake\Documents` (동화 자료 `C:\GameMake\_work\fairy_tales`). 이 폴더들은 구현 저장소가 아닙니다.

---

## 1. 프로젝트 개요

잔혹동화 테마의 2D 횡스크롤 액션 탐험(메트로배니아). 1주차 목표는 **임시 그래픽으로 플레이 가능한 MVP**:

> 이동·점프 학습 → 막힌 길 발견 → 다른 경로 탐험·전투 → 공중 대시 획득 → 재방문 → 새 길 통과 → 도착

스토리·최종 아트·보스·제작 시스템은 1주차 범위 밖입니다 (원문 §2).

## 2. 기술 스택

- **Unity 6000.3.20f1** · URP 17.3.0 (2D) · **Input System 1.19.0**
- `activeInputHandler: 1` — **신규 Input System 전용. 레거시 `Input.GetKey`/`GetAxis` 는 실행 시 예외가 난다**
- 키는 `Assets/InputSystem_Actions.inputactions` 로만 관리한다. 스크립트에 키를 직접 쓰지 않는다
- 물리: Fixed Timestep 0.02 (기본값), Physics2D 중력은 쓰지 않는다 (아래 §3)
- 씬: `Assets/Scenes/SampleScene.unity` 하나 (임시)

## 3. 확정된 구조 결정 (2026-10-26)

| 항목 | 결정 | 이유 |
|---|---|---|
| 캐릭터 물리 | **Rigidbody2D Dynamic + 속도 직접 지정.** `gravityScale 0`, 중력은 코드가 계산. Collision Detection **Continuous**, Interpolate, 마찰 0 재질 | 엔진 충돌을 그대로 쓰면서 가속·중력 곡선은 코드가 쥔다. Continuous 는 대시 관통 방지 |
| 상태 구조 | **`PlayerController` 안의 enum 상태기계 한 곳** (지상·상승·하강·대시·피격·사망) | 1주 범위·6상태에 충분. 불필요한 추상화 금지 (원문 §9) |
| 기존 `CharacterMoveBase.cs` | 삭제 (레거시 Input 사용, 힘 기반 이동이 "빠른 가속·정지" 요구와 맞지 않음) | |

상세 근거: [`docs/decisions/001-character-structure.md`](docs/decisions/001-character-structure.md)

**공격·점프 정점** ([`002`](docs/decisions/002-attack-and-apex.md)) — 공격은 이동 상태를 기다리지 않고 즉시 나간다.
상승 중 공격하면 그 지점이 최고 높이가 되고(수직 속도 0), 정점 구간에서는 중력이 약해진다.
**하강 중력 수치는 사용자 자료를 기다리는 중.** 범위 밖 아이디어는 [`docs/BACKLOG.md`](docs/BACKLOG.md).

**맵** ([`004`](docs/decisions/004-map-structure.md)) — **이어진 하나의 세계를 파츠(지역 조각 프리팹)로 나눠 확장한다.**
백트래킹·지도 확장이 핵심이라 방을 떼어 놓고 페이드로 순간이동시키지 않는다 (페이드는 리스폰에만).
충돌은 `World` 의 Rigidbody2D + CompositeCollider2D(Outlines) 하나로 **파츠 경계까지 합친다** —
파츠에는 Rigidbody2D 를 두지 않는다. `Room` 은 카메라 범위이자 지도 한 칸.
**막는 곡선은 Sprite Shape 를 파츠 `Solid` 의 자식으로 Merge**, **밟는 곡선은 만들지 않는다.**
사각형 스프라이트를 이어 붙여 지형을 만들지 않는다 (이음매에 걸림).

### `Assets/Scripts/` (2026-10-26)

```
MetroidVania.asmdef     어셈블리 하나 (Unity.InputSystem 참조), namespace MetroidVania
CameraFollow            추적 · 앞 공간 · 수직 데드존(위 2 / 아래 0.5) · BoxCollider2D 경계 · SnapToTarget()
Player/
  PlayerTuning          조작감 수치 전부 (SO). 점프는 높이 · 정점 시간으로 입력 → 중력 역산
  PlayerInputReader     InputSystem.actions 를 읽음. 누름은 Consume, UI 용 SetBlocked()
  PlayerMotor           감지 + 속도 적용. 자체 FixedUpdate 없음
  PlayerController      enum 상태기계 (Grounded·Rising·Falling 구현, Dashing·Hurt·Dead 는 자리만)
  PlayerDebugHud        개발용 OnGUI 상태 표시
  PlayerRoomTracker     겹친 방 목록 → 현재 방. RoomChanged(이전, 새) 이벤트
World/
  Room                  트리거 박스 = 방 범위 = 카메라 경계 = 지도 한 칸. SafePoint (decisions/004)
  AttachRuleTile        Solid 용 Rule Tile. 형제 Hazard·Deco 타일맵이 붙은 면을 붙음 면 스프라이트로 바꿈
Soul/
  SoulController        물리 없는 영혼. 플레이어 뒤 → 좌클릭 지점 이동·머묾 → 우클릭 순간 복귀.
                        화면 밖 이동 불가, 사정거리 이탈 시 즉시 재부착(오리아나식) (decisions/003)
```

## 4. 진행 상태

> 마지막 갱신 2026-10-26. 실행해서 확인한 것만 "동작함" 으로 옮긴다.

- **1일차 코드 작성 완료 · 컴파일 확인 · 플레이 검증 안 됨.**
  씬 조립 절차 [`docs/guides/day1-player-setup.md`](docs/guides/day1-player-setup.md), 끝에 확인 목록이 있다
- 입력 액션에 `Dash`(K · Left Shift · RB) 추가, `Attack` 에 J 추가. Status·Pause 는 4일차
- **영혼 코드 작성 · 컴파일 확인 · 플레이 검증 안 됨.** 절차 [`docs/guides/soul-setup.md`](docs/guides/soul-setup.md).
  입력 `SoulMove`(좌클릭) · `SoulRecall`(우클릭) · `Point` 추가, **좌클릭은 `Attack` 에서 뺐다**
- **맵 구조(Room · 방 카메라 경계) 코드 작성 · 컴파일 확인 · 플레이 검증 안 됨.** 절차 [`docs/guides/map-setup.md`](docs/guides/map-setup.md)
- 2026-11-05 기준 디스크의 `SampleScene` 은 10/27 이후 저장되지 않았고 `PlayerTuning` 애셋·Ground 레이어가 없다
  — 인스펙터 설정은 저장 후 다시 확인해야 한다. 플레이어가 안 움직이던 원인이 `PlayerTuning` 미연결이었다
  (지금은 기본값으로 실행되고 경고만 남긴다)
- **2026-11-05 사용자 플레이 확인: 정상 동작.** 가이드별 확인 목록을 항목 단위로 검증한 기록은 아직 없다
- **영혼 = 플레이어의 기억** (몸이 가 본 방에만 진입). 시점 전환·거리 3구간·강제 호출·지역 페이드는
  구현 기획 [`docs/plans/soul-memory.md`](docs/plans/soul-memory.md) 에 정리, 미구현
- 다음 작업: 사용자가 맵 타일·스프라이트를 정리한 뒤 다시 테스트하며 이어 간다
- **게임 컨셉(세계관·스토리·캐릭터)은 사용자가 Codex 에서 작업해 넘겨준다.** 받으면 `docs/` 에 두고 이 파일에서 링크한다.
  그 전에는 컨셉을 임의로 정하지 않는다

## 5. 작업 규칙

- 원문 §3 을 따른다: 매일 실행 가능한 상태 유지, 구현 완료와 **실제 플레이 검증 완료를 구분해 보고**한다.
- 수치를 바꾸면 이유와 최종값을 기록한다 (원문 §3-4, §12-5).
- **아트·맵이 나오기 전에는 점프 거리 같은 수치 계산을 미리 하지 않는다** (2026-10-26).
  점프 관련 값은 플레이 감각으로 조정하는 것이 지금 할 일이다.
- `.unity` 씬과 `.prefab` 은 텍스트로 직접 편집하지 않는다. Unity 에디터 안의 조립은 사용자가 직접 하므로
  **절차서(Hierarchy 계층과 컴포넌트를 빠짐없이)** 를 `docs/guides/` 에 쓴다.
- `.meta` 를 임의로 만들거나 지우지 않는다. 옮길 때는 파일과 쌍으로.
- 결정은 `docs/decisions/` 에 남긴다.

## 6. 아트 생성 파이프라인 (2026-11-06)

- 게임 이미지(캐릭터·애니메이션·리깅·이펙트·배경)는 **`art-pipeline` 스킬부터** 시작한다. 기능별 스킬: `art-master` · `art-rig` · `art-animate` · `art-fx` · `art-postprocess` (`.claude/skills/`)
- 카탈로그 [`docs/art-pipeline/README.md`](docs/art-pipeline/README.md), 후처리 지점 P1–P12 [`docs/art-pipeline/postprocess.md`](docs/art-pipeline/postprocess.md), 결정 [`docs/decisions/005-art-skill-pipeline.md`](docs/decisions/005-art-skill-pipeline.md)
- 외부 도구 7종은 `Tools/ArtSkills/vendor/` (git 무시, `Tools/ArtSkills/setup.ps1` 로 복원). 그 안의 파일은 고치지 않는다
- 이 PC 에는 이미지·영상 생성 경로(API 키, codex/grok CLI)가 없다 — 코드 기반 도구만 바로 동작한다
- **타일 32px · PPU 32 확정 (2026-11-06).** 공통 팔레트는 후보(추운 지옥·불 지옥)만 있다
- **추운 지옥 타일셋: Unity 컴파일·임포트·Build Tiles 확인 (2026-11-07).** Solid 는 `AttachRuleTile` — Hazard·Deco 가 붙으면 면 모양이 바뀐다(빈칸 해소, 씬에서 눈으로 확인). 플레이 검증은 안 됨. 절차 [`docs/guides/cold-hell-tiles-setup.md`](docs/guides/cold-hell-tiles-setup.md)
  (`Assets/Art/Tiles/ColdHell/`, `Assets/Editor/ArtImport/`)
- **불 지옥 타일셋: 추운 지옥과 같은 구조로 생성 · 임포트 · Build Tiles 확인 (2026-11-07).** 씬에 칠해 보기·플레이 검증은 안 됨.
  절차 [`docs/guides/fire-hell-tiles-setup.md`](docs/guides/fire-hell-tiles-setup.md) (`Assets/Art/Tiles/FireHell/`)
