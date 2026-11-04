# 004 — 맵 구조와 곡선 처리

2026-11-05 확정.

## 1. 지형은 Tilemap 한 장에 모은다

```
Level        Grid (1칸 = 1유닛)
  Solid      충돌 지형. TilemapCollider2D → CompositeCollider2D 로 외곽선 하나   Layer Ground
  Hazard     낙사·가시 구역. 트리거 (3일차에 스크립트)
  Background 충돌 없는 배경 표식
Rooms
  Room_*     트리거 박스 = 방 범위 = 카메라 경계. 자식 SafePoint
```

- **사각형 스프라이트를 이어 붙이지 않는다.** 박스 콜라이더가 맞닿은 이음매에서 캡슐이 턱에 걸린다
  (검증표 "모서리 끼임"). Composite 가 충돌을 외곽선 하나로 합쳐 이음매를 없앤다.
- **Composite Geometry Type 은 Outlines.** 외곽선이 이어진 선분 체인으로 만들어져
  미끄러질 때 내부 경계에 걸리지 않는다. Polygons 는 내부를 볼록 조각으로 나눠 조각 경계에 걸릴 수 있다.
- **방마다 Tilemap 을 나누지 않는다.** 방 사이 통로에 다시 이음매가 생긴다.
  방의 경계는 충돌이 아니라 `Room` 트리거로 나눈다.
- 배치는 Tile Palette 로 직접 칠한다. 텍스트·코드 생성은 손으로 설계하는 메트로배니아에 맞지 않는다.
- 틈·턱 크기는 계산하지 않고 칠한 뒤 뛰어 보며 맞춘다 (CLAUDE.md 작업 규칙).

## 2. 방 전환

- `Room` (트리거 BoxCollider2D) — 플레이어가 들어오면 `PlayerRoomTracker` 에 알린다.
- `PlayerRoomTracker` (플레이어 루트) — 겹친 방 목록을 든다. 문턱에서 두 방이 겹쳐도
  새로 들어간 방이 현재 방, 현재 방에서 나가면 아직 겹친 방으로. 모든 방 밖이면 마지막 방 유지.
- `CameraFollow` — 현재 방의 범위를 경계로 쓴다. 방이 바뀌면 경계만 바꾸고 이동은 SmoothDamp 에 맡긴다.
  첫 진입은 바로 맞춘다.
- `Room.SafePoint` — 낙사 복귀 위치. 3일차 Hazard 에서 쓴다.

## 3. 곡선 오브젝트

| 분류 | 도구 | 충돌 |
|---|---|---|
| 직선 지형 | Tilemap `Solid` | Composite 외곽선 |
| **막는 곡선** (바위·동굴 벽·아치) | **Sprite Shape** (닫힌 도형), `Solid` 의 자식 | PolygonCollider2D, Composite Operation **Merge** |
| 배경 곡선 | Sprite Shape 또는 스프라이트 | 없음 |
| **밟는 곡선** | **만들지 않는다** (사용자 결정) | — |

막는 곡선 규칙:

1. **닫힌 면(PolygonCollider2D) 을 쓴다.** 두께 없는 EdgeCollider2D 는 대시·고속 낙하에서 파고들 수 있다.
2. **충돌선을 그림보다 살짝 안쪽에.** 곡선 모서리에서 "안 닿았는데 막혔다" 를 막는다.
3. **`Solid` 의 자식으로 넣고 Merge.** 같은 Rigidbody2D 아래의 충돌이라 Composite 가 타일과 한 외곽선으로 합친다.
   따로 두면 타일과 만나는 곳이 이음매가 된다.
4. **꼭대기에 설 수 있는 오브젝트는 충돌 윗면을 평평하게.** 이동 코드는 45°보다 기운 면을 바닥으로 보지 않고
   마찰이 0 이라 둥근 꼭대기에서는 미끄러진다. 서지 못하게 할 곳이면 둥글게 두고 미끄러짐을 의도로 쓴다.
5. 둥근 천장·비스듬한 벽 밑면은 물리 솔버가 미끄러뜨린다. 천장·벽 감지 기준도 45° 라
   그보다 완만한 면에서 어색하면 플레이 후 조정한다.

## 참고

Unity Korea, "2D 게임 개발 꿀팁 시리즈 — 메모리 절약 스프라이트 5가지" (9-Slice · Tiling · Tilemap · Sprite Shape · Sprite Swap).
곡선 지형을 Sprite Shape 로 그린다는 부분을 따랐다. 영상은 표현·메모리를 다루며 충돌은 위 규칙으로 따로 정했다.
