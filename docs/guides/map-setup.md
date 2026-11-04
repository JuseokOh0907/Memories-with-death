# 맵 조립 — Tilemap · 방 · 막는 곡선

`SampleScene` 의 테스트 바닥(사각형 스프라이트)을 Tilemap 으로 바꾸고, 방(Room)을 나눈다.
구조와 이유는 [`../decisions/004-map-structure.md`](../decisions/004-map-structure.md).

전제: [`day1-player-setup.md`](day1-player-setup.md) 의 `Player` 와 `Ground` 레이어.

표기: `[C]` 는 컴포넌트, 들여쓰기는 부모-자식. 전부 일반 `Transform`.

---

## 0. 준비물 — 임시 타일 2장 + 팔레트 1개

| 만들 것 | 방법 |
|---|---|
| 폴더 `Assets/Art/Temp/` | Project 창 우클릭 `Create > Folder` |
| 스프라이트 `Assets/Art/Temp/Block.png` | 그 폴더에서 `Create > 2D > Sprites > Square` → 이름 `Block`. 1 × 1 유닛 흰 사각형 |
| 팔레트 `Assets/Art/Temp/Prototype` | `Window > 2D > Tile Palette` → `Create New Palette` · 이름 `Prototype` · Grid Rectangle · Cell Size Automatic |
| 타일 `SolidTile` | `Block` 스프라이트를 팔레트 창에 드래그 → 저장 위치 `Assets/Art/Temp/` → 이름 `SolidTile` |
| 타일 `HazardTile` | `SolidTile` 애셋을 `Ctrl+D` 로 복제 → 이름 `HazardTile` → Inspector 의 Color 를 빨강. 팔레트에도 드래그 |

`SolidTile` 의 Color 는 회색 정도로. 배경용 타일이 필요하면 같은 방식으로 `BackgroundTile`(옅은 색) 을 만든다.

## 1. 완성형 계층

씬 루트는 **Main Camera · Global Light 2D · Player · Soul · Level · Rooms** 여섯이다.
day1 가이드의 `Level`(Floor · WallLeft … · CameraBounds) 은 **이 계층으로 대체**한다.
이름이 겹치므로 **시작 전에 옛 것을 `Level_Old` 로 바꿔 두고**, 2절 끝에서 지운다.

```
Level                   2D Object > Tilemap > Rectangular 로 생기는 "Grid" 의 이름을 바꾼다
  [C] Grid              Cell Size (1, 1, 0)
  Solid                 위 메뉴로 함께 생긴 "Tilemap" 의 이름을 바꾼다 · Layer Ground
    [C] Tilemap
    [C] TilemapRenderer
    [C] TilemapCollider2D
    [C] Rigidbody2D
    [C] CompositeCollider2D
    (막는 곡선 오브젝트는 여기 자식으로 — 4절)
  Hazard                Level 우클릭 2D Object > Tilemap > Rectangular · Layer Default
    [C] Tilemap
    [C] TilemapRenderer
    [C] TilemapCollider2D
  Background            Level 우클릭 2D Object > Tilemap > Rectangular · Layer Default
    [C] Tilemap
    [C] TilemapRenderer
Rooms                   Create Empty · Position (0, 0, 0)
  Room_Start            Create Empty (Rooms 의 자식)
    [C] BoxCollider2D
    [C] Room
    SafePoint           Create Empty (Room_Start 의 자식)
  Room_…                (방마다 같은 구성 — 3절)
```

`Level` 아래 게임오브젝트는 **4개**(Level · Solid · Hazard · Background), `Rooms` 아래는 **1 + 방 수 × 2개**다.

## 2. Level

### `Solid` — 충돌 지형

| 컴포넌트 | 설정 |
|---|---|
| TilemapRenderer | Order in Layer **0** |
| TilemapCollider2D | Composite Operation → **Merge** |
| Rigidbody2D | `CompositeCollider2D` 를 붙이면 자동으로 생긴다. Body Type → **Static** |
| CompositeCollider2D | Geometry Type → **Outlines** · Generation Type Synchronous |

- **Layer 는 반드시 Ground.** 플레이어의 `PlayerMotor` → Solid Layers 가 Ground 만 본다.
- Outlines 로 두는 이유: 외곽선이 이어진 선분 체인이 되어, 벽을 따라 미끄러질 때 내부 조각 경계에 걸리지 않는다.
- Scene 뷰에서 타일 덩어리 둘레에 **초록 외곽선 하나**만 보이면 맞다. 타일마다 사각형이 보이면 Merge 가 안 된 것이다.

### `Hazard` — 낙사·가시 구역

- TilemapRenderer Order in Layer **1**
- TilemapCollider2D → **Is Trigger ✔**
- 지금은 표시만 된다. 피해·복귀는 3일차 Hazard 스크립트를 붙일 때 동작한다

### `Background` — 배경 표식

- TilemapRenderer Order in Layer **-10**. 충돌 컴포넌트 없음
- 갈림길·막힌 길을 기억하게 하는 표식용 (인계 문서 §7 "지형·표식으로 기억")

### 칠하기

Tile Palette 창 상단 **Active Tilemap** 을 바꿔 가며 칠한다 (`Solid` 에 SolidTile, `Hazard` 에 HazardTile).
레이아웃은 인계 문서 §7 의 흐름을 따른다:

```
[시작·체크포인트] ─ [갈림길] ─ [대시 필요 틈] ─ [도착]
                       │
                  [점프·전투]
                       │
                  [대시 획득]
                       │
                  [갈림길로 돌아오는 지름길]
```

틈·턱의 크기는 계산하지 않는다. 칠한 뒤 직접 뛰어 보고 칸을 지우거나 더해 맞춘다.

다 칠하고 플레이어가 새 바닥 위에서 서는 것을 확인했으면, **day1 의 옛 `Level_Old`(사각형들과 CameraBounds) 를 지운다.**

## 3. Rooms

방 하나 = 트리거 박스 하나 = 카메라가 보여줄 범위.

| 오브젝트 | 설정 |
|---|---|
| `Room_*` | BoxCollider2D **Is Trigger ✔** (Room 이 실행 시에도 켠다) · Offset·Size 로 방 전체를 덮는다 |
| `Room` | Room Id → 비우면 오브젝트 이름 · Safe Point → 자식 **SafePoint** |
| `SafePoint` | 그 방에서 낙사했을 때 플레이어 **발이 놓일 자리**. 평평한 바닥 위, 다시 떨어지지 않는 곳 |

- **이웃한 방의 박스는 문턱에서 조금 겹치게.** 틈이 있으면 그 사이에서 현재 방이 바뀌지 않는다.
  겹친 구간에서는 새로 들어간 방이 현재 방이 된다
- 방이 화면보다 작으면 카메라는 그 방 가운데에 고정된다
- Scene 뷰에서 방 범위는 **하늘색 상자**, SafePoint 는 **초록 원**으로 보인다 (`Room` 기즈모)
- 이름 예: `Room_Start` · `Room_Fork` · `Room_DashGap` · `Room_Goal` · `Room_Combat` · `Room_DashAltar` · `Room_Shortcut`

### Player 에 컴포넌트 하나 추가

```
Player
  [C] PlayerRoomTracker   ← 추가. 채울 필드 없음
```

이게 있어야 `Room` 이 플레이어를 알아보고, `CameraFollow` 가 방 범위를 경계로 쓴다.

### Main Camera

`CameraFollow` → **Bounds 는 비운다.** 방에 들어가는 순간 방 범위로 채워진다.
(방이 하나도 없는 씬에서만 직접 넣는다.)

## 4. 막는 곡선 — Sprite Shape

둥근 바위, 동굴 벽, 아치처럼 **플레이어를 막기만 하는** 곡선. 밟는 곡선은 만들지 않는다.

### 준비물 (한 번만)

| 만들 것 | 방법 |
|---|---|
| `Assets/Art/Temp/Block.png` 의 Wrap Mode | `Block` 선택 → Inspector **Wrap Mode → Repeat** → Apply (채우기 무늬가 반복되도록) |
| 프로필 `Assets/Art/Temp/RockShape` | `Create > 2D > Sprite Shape Profile > Closed Shape` → Fill Texture 에 `Block` |

### 오브젝트

```
Solid                           (기존)
  Rock_01                       Solid 우클릭 2D Object > Sprite Shape > Closed Shape · Layer Ground
    [C] SpriteShapeController   Profile → RockShape
    [C] SpriteShapeRenderer
    [C] PolygonCollider2D       ← Add Component
```

| 설정 | 값 |
|---|---|
| 부모 | **반드시 `Solid` 의 자식** — 같은 Rigidbody2D 아래여야 Composite 가 합친다 |
| Layer | **Ground** |
| PolygonCollider2D → Composite Operation | **Merge** |
| SpriteShapeController → Collider 항목 | Update Collider ✔ · Collider Detail 는 낮게 시작 · **Collider Offset 을 조절해 초록 충돌선이 그림 안쪽으로 살짝 들어가게** |

- Scene 뷰에서 점을 드래그해 모양을 만든다. 점을 선택하고 Tangent Mode 를 Continuous 로 바꾸면 곡선이 된다
- 확인: 바위와 타일이 맞닿은 곳에 **초록 선이 끊기지 않고 이어져야** 한다. 바위 둘레에 따로 닫힌 선이 보이면
  부모가 `Solid` 가 아니거나 Merge 가 아닌 것이다
- **꼭대기에 올라설 수 있는 바위는 윗면 점들을 수평으로 맞춘다.** 둥글게 두면 미끄러져 내려온다 —
  서지 못하게 할 곳이면 그대로 둔다

## 5. 확인할 것

- [ ] `Solid` 둘레에 초록 외곽선 하나. 타일마다 사각형이 보이지 않는다
- [ ] 벽을 따라 미끄러지며 떨어질 때 타일 경계에서 걸리지 않는다
- [ ] 바닥을 달리다가 타일 경계에서 멈칫하지 않는다
- [ ] 방을 옮기면 카메라가 새 방 범위로 부드럽게 넘어가고, 방 밖(벽 너머)을 보여주지 않는다
- [ ] 시작하자마자 카메라가 첫 방에 바로 맞춰져 있다 (날아오지 않는다)
- [ ] 디버그 표시의 `Room` 줄이 현재 방 이름으로 바뀐다
- [ ] 문턱에서 앞뒤로 왔다 갔다 해도 방이 이상하게 튀지 않는다
- [ ] 곡선 바위에 부딪혀도 "안 닿았는데 막힌" 느낌이 없다. 바위와 바닥 경계에서 걸리지 않는다
- [ ] 대시나 높은 곳에서의 낙하로 곡선 바위를 뚫지 않는다 (대시는 4일차)
- [ ] 영혼은 벽·바위를 그대로 통과한다
