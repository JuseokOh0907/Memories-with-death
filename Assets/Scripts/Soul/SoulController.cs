using UnityEngine;

namespace MetroidVania
{
    /// <summary>
    /// 플레이어에게 붙어 다니는 영혼. 물리 바깥에서 움직인다 — 벽을 지나고 플레이어를 밀지 않는다.
    /// 평소에는 플레이어 살짝 뒤에 떠 있고, 좌클릭하면 그 지점으로 날아가 머문다.
    /// 우클릭하면 플레이어 뒤로 순간이동해 다시 따라다닌다.
    /// 화면 밖으로는 갈 수 없고, 사정거리(반경·화면)를 벗어나면 오리아나 구체처럼 즉시 다시 붙는다.
    /// </summary>
    public class SoulController : MonoBehaviour
    {
        private enum Mode
        {
            Following,  // 플레이어 뒤 자리를 따라다닌다
            Moving,     // 클릭 지점으로 가는 중
            Anchored,   // 클릭 지점에 머문다
        }

        [SerializeField] private PlayerController player;
        [Tooltip("비워 두면 Camera.main")]
        [SerializeField] private Camera worldCamera;

        [Header("기본 자리")]
        [Tooltip("플레이어 위치 기준. x 는 바라보는 반대쪽(뒤)으로 적용된다")]
        [SerializeField] private Vector2 restOffset = new Vector2(0.7f, 0.9f);
        [Tooltip("뒤 자리를 따라붙는 지연. 클수록 늘어지게 따라온다")]
        [SerializeField, Min(0f)] private float followSmoothTime = 0.12f;

        [Header("클릭 지점 이동")]
        [SerializeField, Min(0f)] private float moveSmoothTime = 0.25f;
        [SerializeField, Min(0.1f)] private float moveMaxSpeed = 14f;
        [Tooltip("목표와 이만큼 가까워지면 도착으로 본다")]
        [SerializeField, Min(0.001f)] private float arriveDistance = 0.02f;

        [Header("사정거리 — 반경과 화면 둘 다")]
        [Tooltip("플레이어와 이보다 멀어지면 즉시 플레이어 뒤로 다시 붙는다. " +
                 "이보다 먼 곳을 클릭하면 이 반경 끝까지만 간다")]
        [SerializeField, Min(0.5f)] private float maxDistance = 8f;
        [Tooltip("클릭 이동은 화면 가장자리에서 이만큼 안쪽까지만 간다. 카메라가 움직여 영혼이 화면 밖으로 나가면 다시 붙는다")]
        [SerializeField, Min(0f)] private float screenMargin = 0.5f;

        private const float BoundaryInset = 0.05f;

        private PlayerInputReader input;
        private Mode mode;
        private Vector2 target;
        private Vector2 velocity;

        private Vector2 PlayerPosition => player.transform.position;

        private Vector2 RestPoint =>
            PlayerPosition + new Vector2(-player.Facing * restOffset.x, restOffset.y);

        private void Awake()
        {
            if (player == null)
            {
                Debug.LogError($"{name}: Player 가 비어 있다.", this);
                enabled = false;
                return;
            }

            input = player.GetComponent<PlayerInputReader>();
            if (worldCamera == null)
                worldCamera = Camera.main;

            if (GetComponent<Rigidbody2D>() != null || GetComponent<Collider2D>() != null)
                Debug.LogWarning($"{name}: 영혼은 물리 없이 움직인다. Rigidbody2D·Collider2D 를 지워야 플레이어를 밀지 않는다.", this);
        }

        private void Start() => Recall();

        private void Update()
        {
            // 입력 차단(UI)은 PlayerInputReader 가 이미 처리한다.
            if (input.ConsumeSoulRecallPressed())
                Recall();
            if (input.ConsumeSoulMovePressed())
                MoveTo(ScreenToWorld(input.PointerScreenPosition));

            float dt = Time.deltaTime;
            Vector2 pos = transform.position;

            // 사정거리 이탈 — 오리아나 구체처럼 날아오지 않고 즉시 플레이어에 다시 붙는다.
            // 클릭 목표는 반경·화면 안으로 잘라 두므로 이 경우는 플레이어(와 카메라)가 움직였을 때만 생긴다.
            if (mode != Mode.Following && IsOutOfRange(pos))
            {
                Recall();
                return;
            }

            switch (mode)
            {
                case Mode.Following:
                    pos = Vector2.SmoothDamp(pos, RestPoint, ref velocity, followSmoothTime, Mathf.Infinity, dt);
                    break;

                case Mode.Moving:
                    pos = Vector2.SmoothDamp(pos, target, ref velocity, moveSmoothTime, moveMaxSpeed, dt);
                    if ((pos - target).sqrMagnitude <= arriveDistance * arriveDistance)
                    {
                        pos = target;
                        velocity = Vector2.zero;
                        mode = Mode.Anchored;
                    }
                    break;
            }

            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }

        /// <summary>
        /// 지정한 월드 지점으로 날아가 머문다. 가는 도중에 다시 부르면 목표만 바뀐다.
        /// 사정거리 밖이면 반경 끝까지, 화면 밖이면 화면 가장자리까지만 간다
        /// — 가다가 사정거리 이탈로 붙어 버리는 일이 없게.
        /// </summary>
        public void MoveTo(Vector2 worldPoint)
        {
            Vector2 center = PlayerPosition;
            // 경계에 딱 맞추면 도착하자마자 부동소수 오차로 이탈 판정이 날 수 있어 살짝 안쪽으로 자른다.
            Vector2 point = center + Vector2.ClampMagnitude(worldPoint - center, maxDistance - BoundaryInset);

            Rect view = VisibleRect(screenMargin);
            point.x = Mathf.Clamp(point.x, view.xMin, view.xMax);
            point.y = Mathf.Clamp(point.y, view.yMin, view.yMax);

            target = point;
            mode = Mode.Moving;
        }

        /// <summary>플레이어 뒤 자리로 순간이동하고 다시 따라다닌다.</summary>
        public void Recall()
        {
            Vector2 rest = RestPoint;
            transform.position = new Vector3(rest.x, rest.y, transform.position.z);
            velocity = Vector2.zero;
            mode = Mode.Following;
        }

        /// <summary>
        /// 사정거리는 반경과 화면 둘 다다. 화면은 여백 없이 실제 가장자리로 판정한다 —
        /// 이동은 여백 안쪽까지만 하므로 그 차이만큼 여유가 생겨 경계에서 깜빡이지 않는다.
        /// </summary>
        private bool IsOutOfRange(Vector2 pos) =>
            (pos - PlayerPosition).sqrMagnitude > maxDistance * maxDistance || !VisibleRect(0f).Contains(pos);

        /// <summary>카메라에 보이는 월드 영역에서 inset 만큼 안쪽.</summary>
        private Rect VisibleRect(float inset)
        {
            float halfH = worldCamera.orthographicSize - inset;
            float halfW = worldCamera.orthographicSize * worldCamera.aspect - inset;
            Vector2 c = worldCamera.transform.position;
            return Rect.MinMaxRect(c.x - halfW, c.y - halfH, c.x + halfW, c.y + halfH);
        }

        private Vector2 ScreenToWorld(Vector2 screen) =>
            worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
    }
}
