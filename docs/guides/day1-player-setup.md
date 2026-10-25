# 1일차 — 플레이어와 테스트 바닥 조립

목표: `SampleScene` 에서 좌우 이동 · 가변 점프 · 코요테 타임 · 입력 버퍼 · 벽/천장 충돌을 직접 확인한다.
코드는 `Assets/Scripts/` 에 있고, 이 문서는 에디터에서 손으로 조립하는 절차만 다룬다.

표기: `[C]` 는 컴포넌트, 들여쓰기는 부모-자식. 전부 일반 `Transform` 이다 (Canvas 없음).

---

## 0. 준비물 (에셋 2개 + 레이어 1개)

| 만들 것 | 메뉴 | 설정 |
|---|---|---|
| 레이어 `Ground` | Inspector 우상단 `Layer > Add Layer…` → 빈 칸(User Layer 6)에 입력 | |
| `Assets/Settings/PlayerTuning.asset` | Project 창 우클릭 `Create > MetroidVania > Player Tuning` | 기본값 그대로 (§4 참조) |
| `Assets/Settings/NoFriction.physicsMaterial2D` | `Create > 2D > Physics Material 2D` | Friction **0**, Bounciness **0** |

마찰 0 재질이 없으면 공중에서 벽에 대고 방향키를 누를 때 벽에 달라붙는다.

## 1. 완성형 계층

씬 루트는 **Main Camera · Global Light 2D · Player · Level** 넷이다. 앞의 둘은 이미 있다.

```
Main Camera                 (기존)
  [C] Camera                Projection Orthographic · Size 7
  [C] CameraFollow          ← 추가
Global Light 2D             (기존, 손대지 않음)
Player                      Create Empty · Position (0, 1, 0) · Layer Default
  [C] Rigidbody2D
  [C] CapsuleCollider2D
  [C] PlayerInputReader
  [C] PlayerMotor
  [C] PlayerController
  [C] PlayerDebugHud
  Visual                    Create Empty (Player 의 자식)
    [C] SpriteRenderer
    FacingMark              2D Object > Sprites > Square (Visual 의 자식)
      [C] SpriteRenderer
Level                       Create Empty · Position (0, 0, 0)
  Floor                     2D Object > Sprites > Square · Layer Ground
    [C] SpriteRenderer
    [C] BoxCollider2D
  WallLeft                  (Floor 와 같은 구성)
  WallRight                 (Floor 와 같은 구성)
  Ceiling                   (Floor 와 같은 구성)
  Step                      (Floor 와 같은 구성)
  ThinPlatform              (Floor 와 같은 구성)
  CameraBounds              Create Empty · Layer Default
    [C] BoxCollider2D       Is Trigger ✔
```

게임오브젝트는 모두 **13개**다: Main Camera, Global Light 2D, Player, Visual, FacingMark,
Level, Floor, WallLeft, WallRight, Ceiling, Step, ThinPlatform, CameraBounds.

## 2. Player

### 루트 `Player`

`Add Component` 를 **PlayerController 부터** 누르면 `RequireComponent` 때문에
Rigidbody2D · PlayerMotor · PlayerInputReader 가 함께 붙는다. CapsuleCollider2D 와 PlayerDebugHud 는 따로 붙인다.

| 컴포넌트 | 채울 것 | 비워도 되는 것 |
|---|---|---|
| Rigidbody2D | 없음 | Body Type · Gravity Scale · Interpolate · Collision Detection · Freeze Rotation 은 **`PlayerMotor` 가 실행 시 덮어쓴다** |
| CapsuleCollider2D | Size **(0.8, 1.6)** · Offset **(0, 0.8)** · Direction Vertical · Material **NoFriction** | |
| PlayerInputReader | 없음 | Move Dead Zone 0.25. 입력은 프로젝트 전역 액션을 자동으로 쓴다 |
| PlayerMotor | Solid Layers → **Ground 만** 체크 | |
| PlayerController | Tuning → **PlayerTuning** · Visual → **Visual** | |
| PlayerDebugHud | 없음 | Show ✔ (화면 우상단에 상태 표시) |

Offset 0.8 은 **루트 위치 = 발바닥** 으로 맞추기 위한 값이다. 카메라와 이후 리스폰 위치가 이 기준을 쓴다.

### 자식 `Visual`

- Position **(0, 0.8, 0)** · Scale **(0.8, 0.8, 1)**
- SpriteRenderer → Sprite: 내장 **Capsule** (`2D Object > Sprites > Capsule` 로 만든 것의 스프라이트, 1 × 2 유닛) · Color 아무거나

Scale 0.8 이 곱해져 0.8 × 1.6 이 되어 충돌체와 겹친다. 나중에 아트를 바꿀 때는 **이 오브젝트만** 손댄다.

### 손자 `FacingMark`

- Position **(0.3, 0.4, 0)** · Scale **(0.25, 0.25, 1)** · Color 검정
- Order in Layer **1** (몸통 위에 그려지게)

캡슐은 좌우 대칭이라 바라보는 방향이 안 보인다. 이 점이 그걸 보여준다.

## 3. Level

전부 `2D Object > Sprites > Square` 로 만든 뒤 Layer 를 **Ground** 로 바꾸고 `BoxCollider2D` 를 붙인다.
내장 Square 는 1 × 1 유닛이라 **Scale 이 곧 크기**다. BoxCollider2D 는 Size (1, 1) 그대로 둔다.

| 이름 | Position | Scale | 무엇을 확인하나 |
|---|---|---|---|
| Floor | (0, -0.5) | (40, 1) | 윗면이 y = 0. 기본 이동·착지 |
| WallLeft | (-20.5, 5) | (1, 12) | 벽 충돌, 벽에 대고 점프해도 끼지 않는지 |
| WallRight | (20.5, 5) | (1, 12) | 〃 |
| Ceiling | (-10, 3) | (6, 0.5) | 아래면 y = 2.75. 밑에서 점프하면 머리를 박고 바로 내려오는지 |
| Step | (8, 1) | (4, 2) | 윗면 y = 2. 발판 끝에서 떨어지며 코요테 점프 |
| ThinPlatform | (14, 3.5) | (3, 0.1) | 두께 0.1 — 고속 낙하로 뚫리지 않는지 |

### `CameraBounds`

- Position (0, 5) · BoxCollider2D **Is Trigger ✔** · Size **(42, 14)**
- 벽 바깥이 화면에 보이지 않으면 맞게 된 것이다

Layer 를 Ground 로 두지 **않는다**. 트리거라 충돌 감지에서 빠지지만, 레이어까지 분리해 두면 헷갈리지 않는다.

## 4. Main Camera

- Camera → Size **7** (화면 높이 14 유닛, 캐릭터 1.6 유닛 ≈ 11%)
- `CameraFollow` → Target **Player** · Bounds **CameraBounds**
- 나머지는 기본값

## 5. 확인할 것

실행해서 직접 해 본 것만 체크한다. 우상단 디버그 표시로 상태·접지·속도를 같이 본다.

- [ ] A/D·방향키로 좌우 이동. 손을 떼면 바로 멈추고 `FacingMark` 가 방향을 따라간다
- [ ] Space 를 짧게 누른 점프와 끝까지 누른 점프의 높이가 확실히 다르다 (긴 점프 ≈ 3.5 유닛)
- [ ] Space 를 누른 채 착지해도 다시 뛰지 않는다
- [ ] `Step` 끝에서 걸어 나간 직후 Space → 점프가 나간다 (코요테 0.1초)
- [ ] 착지 직전에 누른 Space 가 착지하자마자 나간다 (버퍼 0.1초)
- [ ] `Ceiling` 밑에서 점프하면 머리를 박고 바로 떨어진다. State 가 Rising 에 머물지 않는다
- [ ] 공중에서 벽에 대고 방향키를 눌러도 벽에 붙지 않고 떨어진다
- [ ] 벽·모서리에서 Grounded 가 계속 True 로 남는 "무한 접지" 가 없다
- [ ] `ThinPlatform` 위로 높은 곳에서 떨어져도 뚫리지 않는다
- [ ] 카메라가 벽 바깥을 보여주지 않고, 작은 점프에 위아래로 출렁이지 않는다
- [ ] 떨어질 때 착지할 바닥이 화면에 보인다

FPS 차이 확인(30/60/120)은 프레임 제한 도구를 붙이는 날 함께 한다 — 검증표 §11.

## 6. 기본 수치에서 나오는 거리 (맵 설계용)

| 값 | 계산 | 결과 |
|---|---|---|
| 중력 | 2 × 3.5 / 0.4² | 43.75 |
| 점프 초속 | 43.75 × 0.4 | 17.5 |
| 같은 높이로 내려오는 시간 | √(2 × 3.5 / (43.75 × 1.5)) | 0.33 초 |
| 최대 점프 수평 거리 | (0.4 + 0.33) × 8 | **약 5.8 유닛** |
| 코요테까지 쓴 최대 | + 0.1 × 8 | **약 6.6 유닛** |

"대시 필요 틈" 은 같은 높이 기준 **6.6 유닛보다 확실히 넓게** 잡는다 (2일차 맵 작업 때 대시 거리와 함께 정한다).
