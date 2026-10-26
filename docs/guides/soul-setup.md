# 영혼(Soul) 조립

평소에는 플레이어 살짝 뒤에 떠 있고, **좌클릭한 지점으로 날아가 머문다.** 우클릭하면 플레이어 뒤로 순간이동한다.
코드는 `Assets/Scripts/Soul/SoulController.cs`.

전제: [`day1-player-setup.md`](day1-player-setup.md) 의 `Player` 가 조립돼 있어야 한다
(`PlayerController` · `PlayerInputReader` 가 붙은 오브젝트). 지금 씬의 `Capsule` 은 아직 스크립트가 없다.

표기: `[C]` 는 컴포넌트, 들여쓰기는 부모-자식. 전부 일반 `Transform`.

---

## 1. 계층

`Soul` 은 **씬 루트**에 둔다. `Player` 의 자식으로 넣으면 안 된다 —
플레이어를 따라 순간적으로 같이 움직이고, `Visual` 처럼 좌우 반전까지 물려받아 클릭 지점에 머물 수 없다.

```
Player                  (기존)
Soul                    씬 루트 · Layer Default
  [C] SpriteRenderer
  [C] SoulController    ← 추가
```

게임오브젝트는 `Soul` **1개**다 (자식 없음).

## 2. 지금 `Soul` 에서 지울 것

현재 `Soul` 에는 **Rigidbody2D(Dynamic) 와 CapsuleCollider2D** 가 붙어 있다.
둘 다 `Remove Component` 로 지운다.

- 남겨 두면 중력으로 떨어지고, 플레이어와 같은 자리에서 서로 밀어낸다
- 영혼은 물리 바깥에서 `Transform` 으로만 움직인다. 벽도 통과한다
- 남아 있으면 실행 시 콘솔에 경고가 뜬다

나중에 영혼이 무언가에 닿았는지 알아야 하면 그때 Kinematic + 트리거로 다시 붙인다.

## 3. 컴포넌트 설정

### SpriteRenderer

- Sprite: 지금 쓰는 내장 Capsule 그대로
- Color: 반투명 (예: Alpha 120~150) — 플레이어와 구분되게
- Order in Layer: **-1** (플레이어 뒤에 그려지게)
- Transform Scale: (0.5, 0.5, 1) 정도. 플레이어보다 작게

### SoulController

| 필드 | 채울 것 | 기본값 |
|---|---|---|
| Player | **Player** (`PlayerController` 가 붙은 오브젝트) | — 비우면 에러 후 꺼짐 |
| World Camera | 비워도 됨 | `Camera.main` 자동 |
| Rest Offset | | (0.7, 0.9). 플레이어 발 기준. x 는 **바라보는 반대쪽**으로 적용 |
| Follow Smooth Time | | 0.12. 클수록 늘어지게 따라온다 |
| Move Smooth Time · Move Max Speed | | 0.25 · 14. 클릭 지점으로 가는 감속과 최고 속도 |
| Arrive Distance | | 0.02 |
| Max Distance | | 8. 플레이어와 이보다 멀어지면 날아서 돌아온다. 이보다 먼 곳을 클릭하면 반경 끝까지만 간다 |

시작 위치는 상관없다. 실행하면 `Start` 에서 플레이어 뒤로 순간이동한다.

## 4. 입력

입력 액션에 다음을 추가했다 (`Player` 맵).

| 액션 | 바인딩 |
|---|---|
| `SoulMove` | 마우스 좌클릭 |
| `SoulRecall` | 마우스 우클릭 |
| `Point` | 포인터 위치 |

**`Attack` 에서 마우스 좌클릭을 뺐다.** 공격은 J · Enter.

## 5. 확인할 것

- [ ] 실행 직후 영혼이 플레이어 뒤쪽 위에 떠 있다
- [ ] 좌우로 방향을 바꾸면 영혼이 반대쪽(뒤)으로 넘어간다
- [ ] 영혼이 플레이어를 밀거나 떨어지지 않는다
- [ ] 좌클릭한 지점으로 날아가 **그 자리에 머문다.** 플레이어가 조금 움직여도 따라오지 않는다
- [ ] 머문 영혼에게서 플레이어가 8 유닛 넘게 멀어지면 영혼이 **날아서** 플레이어 뒤로 돌아온다
- [ ] 8 유닛보다 먼 곳을 클릭하면 그 방향으로 반경 끝까지만 가서 머문다 (가다가 되돌아오지 않는다)
- [ ] 날아가는 도중 다시 좌클릭하면 새 지점으로 방향을 바꾼다
- [ ] 벽 너머를 클릭해도 그대로 통과해 간다
- [ ] 우클릭하면 플레이어 뒤로 바로 돌아오고 다시 따라다닌다
- [ ] 좌클릭으로 공격이 나가지 않는다
