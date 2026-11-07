# 추운 지옥 타일셋 — Unity 조립 절차 (2026-11-06)

> 맵 구조는 [`decisions/004`](../decisions/004-map-structure.md)를 따른다. 타일 32px · PPU 32 · Grid 1칸 = 1유닛.
> 원본(재생성용): `Tools/ArtSkills/_work/tiles-cold-hell/` (git 무시). 팔레트 [`art-pipeline/palettes/cold-hell.md`](../art-pipeline/palettes/cold-hell.md).

## 0. 들어온 파일

```
Assets/Art/Tiles/ColdHell/
  ColdHell_FrostStone.png  + .tiles.json   서리 바위 — 47 blob × 5 변형, 충돌 모양 포함  → Solid
  ColdHell_BackWall.png    + .tiles.json   배경 벽   — 47 blob × 2 변형, 충돌 없음       → Background
  ColdHell_Props.png       + .tiles.json   얼음 가시(바닥/천장, 충돌), 고드름, 망령 결정, 뼈
                                           (바닥 가시·결정·뼈는 4px 낮게 그려져 눈 선 위에 앉는다)
  ColdHell_FrostStone_FlushN/S/NS.png + .tiles.json
                                           서리 바위의 "붙음 면" 47종 × 3 — 타일 에셋은 따로 안 생긴다 (§2-1)
Assets/Scripts/World/AttachRuleTile.cs     Solid 용 Rule Tile: 형제 Hazard·Deco 타일맵을 보고 면 모양을 바꾼다
Assets/Editor/ArtImport/                   에디터 전용 코드 (빌드에 안 들어감)
  PixelArtTexturePostprocessor.cs          Assets/Art 아래 PNG 를 픽셀 설정으로 임포트, .tiles.json 대로 자르고 Physics Shape 지정
  TileAssetBuilder.cs                      메뉴 Tools ▸ Art ▸ Build Tiles From Manifests
  TileManifest.cs, MetroidVania.ArtImport.Editor.asmdef
```

`.tiles.json` 은 `Tools/ArtSkills/scripts/atlas_to_unity.py` 가 Pixelbolt 아틀라스에서 만든다. 손으로 고치지 않는다.

## 1. 임포트 확인

1. Unity 로 돌아가 컴파일·임포트가 끝나기를 기다린다. Console 에 `error CS` 가 없어야 한다.
2. `ColdHell_FrostStone.png` 를 선택 → Inspector 확인:
   Texture Type **Sprite (2D and UI)** · Sprite Mode **Multiple** · Pixels Per Unit **32** · Mesh Type **Full Rect** ·
   Filter Mode **Point (no filter)** · Compression **None** · Generate Mip Maps 끔.
3. 펼침 화살표를 열어 스프라이트가 `frost-0-a` … 로 **235개** 있는지 본다 (BackWall 94, Props 5).
   - 하나짜리 스프라이트로 들어왔다면(코드보다 텍스처가 먼저 임포트된 경우) PNG 우클릭 → **Reimport**.
4. Sprite Editor ▸ **Custom Physics Shape** 에서 `frost-255-a` 는 칸 전체, `frost-0-a` 는 윗면 3px·아래 고드름 8px 를 뺀 사각형이어야 한다.

## 2. 타일 에셋 만들기

메뉴 **Tools ▸ Art ▸ Build Tiles From Manifests** 를 누른다. Console 에 아래가 찍혀야 한다.

```
[TileAssetBuilder] Assets/Art/Tiles/ColdHell/ColdHell_FrostStone_Rule.asset: 47 rules
[TileAssetBuilder] Assets/Art/Tiles/ColdHell/ColdHell_BackWall_Rule.asset: 47 rules
[TileAssetBuilder] built tiles from 3 manifest(s)
```

생기는 에셋:
- `ColdHell_FrostStone_Rule.asset` — Rule Tile. 이웃에 맞춰 47종이 자동으로 골라지고, 같은 모양 안에서 변형(a·b·균열·해골·영혼)이 무작위로 섞인다. 충돌 **Sprite**(위 Physics Shape).
- `ColdHell_BackWall_Rule.asset` — Rule Tile, 충돌 **None**.
- `Tiles/spikes-floor.asset`, `Tiles/spikes-ceiling.asset` (충돌 Sprite), `Tiles/icicles.asset`, `Tiles/soul-crystal.asset`, `Tiles/bones.asset` (충돌 None).

다시 눌러도 같은 에셋을 갱신하므로 이미 칠한 타일은 유지된다. 끝나면 열린 씬의 타일맵을 전부 다시 그린다(씬 데이터는 안 바뀐다).

### 2-1. 가시·장식이 붙는 면 (2026-11-07)

Rule Tile 은 원래 **같은 타일맵** 이웃만 본다. 그래서 천장 아래 Hazard 에 가시를 달면 바위 아랫면의 고드름 털 여백(8px)이
그대로 남아 바위와 가시 사이가 비었다. `ColdHell_FrostStone_Rule` 은 이제 **AttachRuleTile** 이다.
빌더가 `ColdHell_FrostStone_Flush*.tiles.json` 을 찾으면 기존 에셋의 클래스를 그 자리에서 바꾼다(GUID 유지, 칠한 타일 유지).
Console 에 `RuleTile → AttachRuleTile (same GUID)` 한 번, 이후 `47 rules, 47 flush masks from 3 set(s)`.

- 같은 Grid 아래 이름이 **`Hazard`·`Deco` 로 시작하는** 형제 타일맵에 타일이 있으면, 그 쪽으로 열린 Solid 면을 바꾼다.
  - **아랫면**(천장 가시·고드름이 매달림): 바위가 칸 끝까지 내려오고 고드름 털이 빠진다 → 빈칸 없음.
  - **윗면**(바닥 가시·뼈·결정이 놓임): 높이는 그대로, 눈 뭉치 없이 평평해진다. 소품 쪽이 4px 내려와 눈 선에 앉는다.
- Hazard·Deco 에 칠하거나 지우면 위아래 Solid 칸이 자동으로 다시 그려진다(`Tilemap.tilemapTileChanged`).
- 타일맵 이름을 바꾸면 감지가 끊긴다. 다른 이름을 쓰려면 Rule 에셋 Inspector 의 **Attach Layers** 에 추가한다.
- 원본: `Tools/ArtSkills/_work/tiles-cold-hell/make_flush.py` → `frost-flush-{n,s,ns}.autotile.json`, 미리보기 `flush-preview.png`.

## 3. Tile Palette

1. Window ▸ 2D ▸ **Tile Palette** → Create New Palette: 이름 `ColdHell`, Grid **Rectangle**, Cell Size **Manual 1 × 1**. 저장 위치 `Assets/Art/Tiles/ColdHell/Palette/`.
2. 위 에셋 7개(Rule Tile 2 + Tile 5)를 팔레트 창에 끌어 놓는다.

## 4. 파츠에 배치 (004 계층 그대로)

```
World                     Rigidbody2D(Static) · CompositeCollider2D(Geometry Type: Outlines) · Layer Ground   ← 기존
  Part_ColdHell           Grid (Cell Size 1,1,0) · 위치 정수 좌표 · 프리팹
    Background            Tilemap · TilemapRenderer (Sorting Layer/Order: 가장 뒤, 예 Order -10)        충돌 없음
    Solid                 Tilemap · TilemapRenderer (Order 0) · TilemapCollider2D(Composite Operation: Merge) · Layer Ground
    Hazard                Tilemap · TilemapRenderer (Order 1) · TilemapCollider2D(Is Trigger 켬)
    Deco                  Tilemap · TilemapRenderer (Order 2)                                              충돌 없음 (고드름·결정·뼈)
    Rooms
      Room_*              기존 Room
```

- **Background** 에 `ColdHell_BackWall_Rule` 을 동굴 안쪽 전체에 칠한다. 깊은 허공으로 보일 곳만 비운다.
- **Solid** 에 `ColdHell_FrostStone_Rule` 로 바닥·벽·천장·발판(떠 있는 덩어리)을 칠한다. 경사는 칠하지 않는다(004: 밟는 곡선 없음). 막는 곡선은 기존대로 Sprite Shape.
- **Hazard** 에 `spikes-floor`(바닥 위 칸), `spikes-ceiling`(천장 아래 칸).
- **Deco** 에 `icicles` 는 천장 바로 아래 칸, `soul-crystal`·`bones` 는 바닥 바로 위 칸.
- **어둠 전환 구간에는 아무 타일도 칠하지 않는다.** 그 자리는 Fade in/out 트리거가 들어갈 빈 공간이다 (트리거·페이드 연출은 이 절차 범위 밖).

## 5. 확인 목록

| # | 확인 | 기대 |
|---|---|---|
| 1 | 바위를 3×3 이상 덩어리로 칠한다 | 바깥 테두리만 눈·고드름이 생기고 안쪽은 어두운 돌 |
| 2 | 1칸짜리 떠 있는 바위 | 사방이 닫힌 섬 모양 타일 1장 |
| 3 | 플레이어로 바위 윗면에 선다 | 눈 덮인 선 위에 발이 닿는다 (3px 아래가 충돌선) |
| 4 | 두 덩어리를 이어 붙인 바닥을 달린다 | 이음매에 걸리지 않는다 (Composite Outlines) |
| 5 | 천장 아래를 점프로 친다 | 고드름 끝이 아니라 바위 밑면에서 막힌다 |
| 6 | Scene 뷰 확대 | 흐림·번짐 없이 픽셀이 또렷하다 (Point, 압축 없음) |
| 7 | 가시 위에 선다 | 트리거만 감지되고 몸이 막히지 않는다 (Hazard 처리 코드는 3일차) |
| 8 | 바위 아래 Hazard 에 `spikes-ceiling`, Deco 에 `icicles` | 바위 밑면과 가시·고드름 사이 빈칸이 없다 |
| 9 | 바위 위에 `spikes-floor`·`bones` | 눈 뭉치가 가시 사이로 안 삐져나오고 소품이 눈 선 위에 앉는다 |
| 10 | 8·9 의 가시를 지운다 | 바위 면이 원래(고드름 털·눈 뭉치) 모양으로 돌아온다 |

## 6. 충돌 판정과 그래픽 (2026-11-07)

충돌은 **스프라이트마다 박힌 Physics Shape** 를 쓰고 Collider Type 은 **Sprite** 다. 그림이 바뀌면(규칙·붙음 면 교체)
충돌도 같은 그림에서 따라오고, 소품을 4px 내린 것도 피벗을 옮긴 것이라 충돌이 같이 내려간다. 어긋나는 경우는 이것뿐이다.

| 경우 | 왜 어긋나나 | 규칙 |
|---|---|---|
| 타일·타일맵을 Scale 로 늘림 | 픽셀이 뭉개지고 판정 크기·위치가 의도와 달라짐 | 늘리지 않는다. 길면 칸 단위로 이어 칠한다 |
| Collider Type 을 Grid 로 바꿈 | 그림과 상관없이 칸 전체가 판정 | 빌더가 만든 Sprite 그대로 둔다 |
| 움직이는 가시를 애니메이션 타일로 | 타일맵 충돌은 프레임을 따라가지 않음 | 튀어나오는 함정은 타일 말고 오브젝트로 |
| 이웃이 바뀌었는데 안 다시 그려짐 | 예전 그림·판정이 남음 | 자동 새로고침(§2-1). 이상하면 Build Tiles 를 다시 누른다 |

**일부러 둔 차이 — 가시 판정은 그림보다 작다.** `spikes-floor` 판정은 아래쪽 14px 사각형(가로 30px)이고
`spikes-ceiling` 은 위쪽 14px 다. 가시 끝(최대 20px 높이)을 살짝 스쳤을 때는 맞지 않게 해서 억울한 피격을 줄인다.
판정 크기를 바꾸려면 `Tools/ArtSkills/_work/tiles-cold-hell/make_props.py` 의 `collision` 을 고치고 다시 굽는다.

확인 결과는 CLAUDE.md §4 진행 상태에 "플레이 검증" 으로 옮긴다.
