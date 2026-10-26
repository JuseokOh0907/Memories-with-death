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

## 5. 작업 규칙

- 원문 §3 을 따른다: 매일 실행 가능한 상태 유지, 구현 완료와 **실제 플레이 검증 완료를 구분해 보고**한다.
- 수치를 바꾸면 이유와 최종값을 기록한다 (원문 §3-4, §12-5).
- **아트·맵이 나오기 전에는 점프 거리 같은 수치 계산을 미리 하지 않는다** (2026-10-26).
  점프 관련 값은 플레이 감각으로 조정하는 것이 지금 할 일이다.
- `.unity` 씬과 `.prefab` 은 텍스트로 직접 편집하지 않는다. Unity 에디터 안의 조립은 사용자가 직접 하므로
  **절차서(Hierarchy 계층과 컴포넌트를 빠짐없이)** 를 `docs/guides/` 에 쓴다.
- `.meta` 를 임의로 만들거나 지우지 않는다. 옮길 때는 파일과 쌍으로.
- 결정은 `docs/decisions/` 에 남긴다.
