# 001 — 캐릭터 스크립트 구조

2026-10-26 확정.

## 결정

```
Player
  Rigidbody2D (Dynamic · 중력 0 · Continuous · Interpolate)  CapsuleCollider2D (마찰 0)
  PlayerInputReader  입력 액션 → 값. 누름은 플래그로 쌓고 FixedUpdate 가 Consume
  PlayerMotor        감지(바닥·천장·벽)와 속도 적용만. 자체 FixedUpdate 없음
  PlayerController   enum 상태기계. FixedUpdate 에서 Sense → 판단 → Commit
  PlayerCombat       (3일차) 공격 타이머 · 스윙당 맞은 대상
  Health             (3일차) 플레이어·적 공용
  PlayerAbilities    (4일차) 영구 소유 능력
  Visual             표시 전용 자식. 아트 교체는 여기서만
```

수치는 `PlayerTuning` ScriptableObject 하나에 모은다.

## 이유

- **Dynamic + 속도 직접 지정** — 엔진 충돌 해결을 그대로 쓰고 가속·중력 곡선만 코드가 쥔다.
  Kinematic + 직접 Cast 는 정밀하지만 1주 범위에서 코드량이 2~3배다.
  `Continuous` 로 대시·고속 낙하 관통을 막는다.
- **enum 상태기계 한 곳** — 6상태면 switch 하나로 전이 전체가 한 화면에 보인다. 상태 클래스는 지금은 파일만 늘린다.
- **공격은 이동 상태가 아니다** — 요구사항이 "공격 중 공중 이동·중력 유지" 라서 상태로 만들면 Rising/Falling 과 겹친다.
  컨트롤러는 "Dashing·Hurt·Dead 에서는 공격 불가" 규칙만 가진다.
- **Motor 가 FixedUpdate 를 갖지 않는다** — 두 컴포넌트가 각자 FixedUpdate 를 돌면 실행 순서가 보장되지 않아
  "감지 → 판단 → 적용" 이 한 스텝 어긋날 수 있다.
- **입력은 플래그 + Consume** — 시간 기록(Time.time) 방식은 Update 와 FixedUpdate 의 시계가 달라
  버퍼 나이가 음수가 될 수 있다. 플래그는 프레임 수와 무관하게 정확히 한 번 처리된다.
- **점프는 높이·정점 시간으로 입력** — 중력과 초속을 역산하므로 맵의 틈·턱 크기를 계산할 수 있다.
- **지상에서도 중력을 건다** — 바닥이 받쳐 주므로 솔버가 수직 속도를 0 으로 되돌린다.
  지상에서 중력을 끄면 감지 거리(0.05) 만큼 떠 있는 채로 멈출 수 있다.
- **능력 소유 ≠ 대시 사용 가능** — 소유는 `PlayerAbilities`(저장), 공중 1회 충전은 컨트롤러의 일시 상태.

## 입력 액션 변경

`InputSystem_Actions` 는 프로젝트 전역 액션으로 등록돼 있어 `InputSystem.actions` 로 바로 읽는다 (인스펙터 연결 없음).

- `Player/Dash` 추가 — K · Left Shift(`Sprint` 에서 옮김) · 게임패드 RB
- `Player/Attack` 에 J 추가 (마우스 왼쪽·Enter 는 남김)
- Status(Tab) · Pause(Esc) 는 UI 를 만드는 4일차에 추가한다
