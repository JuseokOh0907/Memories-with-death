# 맵 조립 — World · 파츠 · 방 · 막는 곡선

맵은 **이어진 하나의 세계(`World`)** 이고, 지역 조각(**파츠**)을 이어 붙여 넓혀 간다.
충돌은 `World` 하나로 합쳐 파츠 경계에도 이음매가 없다. 구조와 이유는 [`../decisions/004-map-structure.md`](../decisions/004-map-structure.md).

전제: [`day1-player-setup.md`](day1-player-setup.md) 의 `Player` 와 `Ground` 레이어.

표기: `[C]` 는 컴포넌트, 들여쓰기는 부모-자식. 전부 일반 `Transform`.

---

## 0. 준비물 — 임시 타일 2장 + 팔레트 1개

| 만들 것 | 방법 |
|---|---|
| 폴더 `Assets/Art/Temp/` · `Assets/Prefabs/Parts/` | Project 창 우클릭 `Create > Folder` |
| 스프라이트 `Assets/Art/Temp/Block.png` | 그 폴더에서 `Create > 2D > Sprites > Square` → 이름 `Block`. 1 × 1 유닛 흰 사각형 |
| 팔레트 `Assets/Art/Temp/Prototype` | `Window > 2D > Tile Palette` → `Create New Palette` · 이름 `Prototype` · Grid Rectangle · Cell Size Automatic |
| 타일 `SolidTile` | `Block` 을 팔레트 창에 드래그 → 저장 위치 `Assets/Art/Temp/` → 이름 `SolidTile` · Color 회색 |
| 타일 `HazardTile` | `SolidTile` 을 `Ctrl+D` 로 복제 → 이름 `HazardTile` → Color 빨강 → 팔레트에 드래그 |

배경용 타일이 필요하면 같은 방식으로 `BackgroundTile`(옅은 색).

## 1. 완성형 계층

씬 루트는 **Main Camera · Global Light 2D · Player · Soul · World** 다섯이다.
day1 가이드의 `Level`(사각형들 · CameraBounds) 은 이 구조로 대체한다 — 2절 끝에서 지운다.

```
World                       Create Empty · Position (0, 0, 0) · Layer Ground
  [C] Rigidbody2D
  [C] CompositeCollider2D
  Part_Start                Create Empty (World 의 자식) · Position 정수 좌표
    [C] Grid
    Solid                   Part_Start 우클릭 2D Object > Tilemap > Rectangular · Layer Ground
      [C] Tilemap
      [C] TilemapRenderer
      [C] TilemapCollider2D
      (Rock_* — 막는 곡선, 4절)
    Hazard                  Part_Start 우클릭 2D Object > Tilemap > Rectangular · Layer Default
      [C] Tilemap
      [C] TilemapRenderer
      [C] TilemapCollider2D
    Background              Part_Start 우클릭 2D Object > Tilemap > Rectangular · Layer Default
      [C] Tilemap
      [C] TilemapRenderer
    Rooms                   Create Empty (Part_Start 의 자식)
      Room_Start            Create Empty
        [C] BoxCollider2D
        [C] Room
        SafePoint           Create Empty
  Part_…                    (지역마다 같은 구성)
```

파츠 하나의 게임오브젝트는 **5 + 방 수 × 2개**다 (Part · Solid · Hazard · Background · Rooms + 방마다 Room · SafePoint).

**`Rigidbody2D` · `CompositeCollider2D` 는 `World` 에만 있다.** 파츠·`Solid` 에는 붙이지 않는다 —
붙이면 그 파츠의 충돌이 따로 떨어져 나가 파츠 경계에 이음매가 생긴다.

## 2. World 와 파츠

### `World`

| 컴포넌트 | 설정 |
|---|---|
| Rigidbody2D | `CompositeCollider2D` 를 붙이면 자동으로 생긴다. Body Type → **Static** |
| CompositeCollider2D | Geometry Type → **Outlines** · Generation Type Synchronous |

### 파츠 `Part_*`

- `Add Component > Grid` · Cell Size **(1, 1, 0)**
- **Position 은 정수.** 파츠마다 Grid 가 따로라, 소수점이 있으면 이웃 파츠와 칸이 어긋나 바닥 높이가 미세하게 달라진다
- 이름은 지역 단위로: `Part_Start` · `Part_Fork` · `Part_Descent` …

### `Solid` — 충돌 지형

| 컴포넌트 | 설정 |
|---|---|
| TilemapRenderer | Order in Layer **0** |
| TilemapCollider2D | Composite Operation → **Merge** (World 의 Composite 로 합쳐진다) |

- **Layer 는 반드시 Ground.** `PlayerMotor` → Solid Layers 가 Ground 만 본다
- 확인: Scene 뷰에서 **파츠 경계를 넘어** 초록 외곽선이 끊기지 않고 이어져야 한다.
  타일마다 사각형이 보이거나 파츠 경계에서 선이 끊기면 Merge 가 아니거나 Rigidbody2D 가 파츠 쪽에 있는 것이다

### `Hazard` · `Background`

- `Hazard`: TilemapRenderer Order **1** · TilemapCollider2D **Is Trigger ✔** (Composite Operation None 그대로).
  피해·복귀는 3일차 Hazard 스크립트에서
- `Background`: TilemapRenderer Order **-10**. 충돌 없음. 갈림길·막힌 길을 기억시키는 표식용

### 칠하기

Tile Palette 창 상단 **Active Tilemap** 을 칠할 파츠의 `Solid` / `Hazard` 로 바꿔 가며 칠한다.
**이웃 파츠와 이어지는 바닥·벽은 경계에서 칸이 빈틈없이 맞닿게** 칠한다.

1주차 흐름 (인계 문서 §7) — 파츠 나누기는 예시다:

```
[Part_Start] ─ [Part_Fork] ─ [대시 필요 틈] ─ [도착]
                    │
              [점프·전투]
                    │
              [대시 획득]
                    │
              [Part_Fork 로 돌아오는 지름길]
```

틈·턱의 크기는 계산하지 않는다. 칠한 뒤 직접 뛰어 보고 칸을 지우거나 더해 맞춘다.

### 프리팹으로 저장

파츠를 Project 창 `Assets/Prefabs/Parts/` 로 드래그해 프리팹으로 만든다.
**프리팹 편집 모드에서는 `World` 가 없어 충돌이 합쳐 보이지 않는다** — 합쳐진 외곽선은 씬에서 확인한다.

새 바닥 위에서 플레이어가 서는 것을 확인했으면 **day1 의 옛 `Level`(사각형들과 CameraBounds) 을 지운다.**

## 3. 방 (Room) — 카메라 범위이자 지도 한 칸

방은 충돌이 아니다. 걸어서 이어지는 공간을 **카메라가 보여줄 범위 · 지도에 그려질 칸** 으로 나눈 것이다.
파츠 하나에 방이 여럿 있을 수 있다.

| 오브젝트 | 설정 |
|---|---|
| `Room_*` | BoxCollider2D **Is Trigger ✔** · Offset·Size 로 방 전체를 덮는다 |
| `Room` | Room Id → **파츠 이름을 앞에** (`Start_Entrance`). 세계 전체에서 겹치지 않게 · Safe Point → 자식 **SafePoint** |
| `SafePoint` | 그 방에서 낙사했을 때 플레이어 **발이 놓일 자리**. 평평한 바닥 위, 다시 떨어지지 않는 곳 |

- **이웃한 방의 박스는 문턱에서 조금 겹치게.** 틈이 있으면 그 사이에서 현재 방이 바뀌지 않는다
- 방을 넘어가면 카메라가 새 방 범위로 **부드럽게** 넘어간다 (순간이동·페이드 없음)
- 방이 화면보다 작으면 카메라는 그 방 가운데에 고정된다
- Scene 뷰에서 방 범위는 **하늘색 상자**, SafePoint 는 **초록 원**

### Player · Main Camera

```
Player
  [C] PlayerRoomTracker   ← 추가. 채울 필드 없음
```

`Main Camera` → `CameraFollow` → **Bounds 는 비운다.** 방에 들어가는 순간 방 범위로 채워진다.

## 4. 막는 곡선 — Sprite Shape

둥근 바위, 동굴 벽, 아치처럼 **막기만 하는** 곡선. 밟는 곡선은 만들지 않는다.

### 준비물 (한 번만)

| 만들 것 | 방법 |
|---|---|
| `Block.png` 의 Wrap Mode | `Block` 선택 → Inspector **Wrap Mode → Repeat** → Apply |
| 프로필 `Assets/Art/Temp/RockShape` | `Create > 2D > Sprite Shape Profile > Closed Shape` → Fill Texture 에 `Block` |

### 오브젝트

```
Solid                           (파츠의 Solid)
  Rock_01                       Solid 우클릭 2D Object > Sprite Shape > Closed Shape · Layer Ground
    [C] SpriteShapeController   Profile → RockShape
    [C] SpriteShapeRenderer
    [C] PolygonCollider2D       ← Add Component
```

| 설정 | 값 |
|---|---|
| 부모 | **파츠의 `Solid` 자식** — `World` 의 Rigidbody2D 아래여야 합쳐진다 |
| Layer | **Ground** |
| PolygonCollider2D → Composite Operation | **Merge** |
| SpriteShapeController → Collider | Update Collider ✔ · Collider Detail 낮게 시작 · **Collider Offset 을 조절해 초록 충돌선이 그림 안쪽으로 살짝 들어가게** |

- 점을 드래그해 모양을 만든다. 점을 선택하고 Tangent Mode 를 Continuous 로 바꾸면 곡선이 된다
- 확인: 바위와 타일이 맞닿은 곳에서 **초록 선이 끊기지 않고 이어져야** 한다
- **꼭대기에 올라설 수 있는 바위는 윗면 점들을 수평으로 맞춘다.** 둥글게 두면 미끄러져 내려온다 —
  서지 못하게 할 곳이면 그대로 둔다

## 5. 확인할 것

- [ ] 초록 외곽선이 **파츠 경계를 넘어** 하나로 이어진다. 타일마다 사각형이 보이지 않는다
- [ ] 파츠 경계를 달려서 지날 때 멈칫하거나 걸리지 않는다
- [ ] 벽을 따라 미끄러지며 떨어질 때 타일·파츠 경계에서 걸리지 않는다
- [ ] 방을 옮기면 카메라가 새 방 범위로 부드럽게 넘어가고, 방 밖(벽 너머)을 보여주지 않는다
- [ ] 시작하자마자 카메라가 첫 방에 바로 맞춰져 있다 (날아오지 않는다)
- [ ] 디버그 표시의 `Room` 줄이 현재 방 이름으로 바뀐다
- [ ] 문턱에서 앞뒤로 왔다 갔다 해도 방이 이상하게 튀지 않는다
- [ ] 곡선 바위에 부딪혀도 "안 닿았는데 막힌" 느낌이 없다. 바위와 바닥 경계에서 걸리지 않는다
- [ ] 영혼은 벽·바위를 그대로 통과하고, 방을 넘어가도 플레이어를 따라온다
