# 불 지옥 타일셋 — Unity 조립 절차 (2026-11-07)

> 추운 지옥과 **같은 구조**다. 임포트·Build Tiles·붙음 면·충돌 규칙은 [`cold-hell-tiles-setup.md`](cold-hell-tiles-setup.md) 를 그대로 따르고,
> 여기에는 이름과 그림만 다른 부분을 적는다. 팔레트 [`art-pipeline/palettes/fire-hell.md`](../art-pipeline/palettes/fire-hell.md).
> 원본(재생성용): `Tools/ArtSkills/_work/tiles-fire-hell/` (git 무시).

## 0. 들어온 파일

```
Assets/Art/Tiles/FireHell/
  FireHell_ScorchStone.png + .tiles.json   그을린 현무암 — 47 blob × 5 변형(a·b·해골·용암 맥·균열), 충돌 포함 → Solid
  FireHell_BackWall.png    + .tiles.json   그을음 벽 — 47 blob × 2 변형, 충돌 없음                         → Background
  FireHell_Props.png       + .tiles.json   용암 가시(바닥/천장, 충돌), 용암 방울, 유황 결정, 탄 뼈
                                           (바닥 가시·결정·뼈는 4px 낮게 그려져 재 선 위에 앉는다)
  FireHell_ScorchStone_FlushN/S/NS.png + .tiles.json   붙음 면 47종 × 3 (타일 에셋은 따로 안 생긴다)
```

## 1. 만들어진 타일 에셋 (Build Tiles 실행 결과, 2026-11-07 확인)

| 에셋 | 종류 | 칠할 타일맵 |
|---|---|---|
| `FireHell_ScorchStone_Rule` | **Attach Rule Tile** (47 규칙, 붙음 면 47) | `Solid` |
| `FireHell_BackWall_Rule` | Rule Tile, 충돌 없음 | `Background` |
| `Tiles/magma-spikes-floor` · `Tiles/magma-spikes-ceiling` | Tile, 충돌 Sprite | `Hazard` |
| `Tiles/lava-drips` | Tile, 충돌 없음 (천장 바로 아래) | `Deco` |
| `Tiles/brimstone-crystal` · `Tiles/charred-bones` | Tile, 충돌 없음 (바닥 바로 위) | `Deco` |

추운 지옥 대응: frost-stone → scorch-stone, spikes → magma-spikes, icicles → lava-drips, soul-crystal → brimstone-crystal, bones → charred-bones.
충돌 크기(가시 판정 아래쪽 14px 등)와 붙음 면 동작은 추운 지옥과 같다.

## 2. Tile Palette · 배치

1. Tile Palette 에서 새 팔레트 `FireHell` (Rectangle, Cell Size Manual 1×1), 저장 위치 `Assets/Art/Tiles/FireHell/Palette/`. 위 7개 에셋을 끌어 놓는다.
2. 파츠 계층은 추운 지옥 §4 와 같다: `Part_FireHell` (Grid) 아래 `Background` · `Solid` · `Hazard` · `Deco`.
   **타일맵 이름이 `Hazard`·`Deco` 로 시작해야** 붙음 면이 동작한다.

## 3. 확인 목록

추운 지옥 §5 의 1–10 을 이 타일들로 똑같이 확인한다. 추가로:

| # | 확인 | 기대 |
|---|---|---|
| 11 | 바위 아랫면이 열린 곳 | 용암 방울 털(8px)이 빛나고, 충돌은 바위 밑면에서 막힌다 |
| 12 | Hazard 에 `magma-spikes-ceiling` | 방울 털이 빠지고 바위가 칸 끝까지 내려와 가시에 붙는다 |

확인 결과는 CLAUDE.md §4 진행 상태에 "플레이 검증" 으로 옮긴다.
