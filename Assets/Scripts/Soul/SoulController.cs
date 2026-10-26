using UnityEngine;

namespace MetroidVania
{
    /// <summary>
    /// 플레이어에게 붙어 다니는 영혼. 물리 바깥에서 움직인다 — 벽을 지나고 플레이어를 밀지 않는다.
    /// 평소에는 플레이어 살짝 뒤에 떠 있고, 좌클릭하면 그 지점으로 날아가 머문다.
    /// 우클릭하면 플레이어 뒤로 순간이동해 다시 따라다닌다.
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

        private PlayerInputReader input;
        private Mode mode;
        private Vector2 target;
        private Vector2 velocity;

        private Vector2 RestPoint =>
            (Vector2)player.transform.position + new Vector2(-player.Facing * restOffset.x, restOffset.y);

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

        /// <summary>지정한 월드 지점으로 날아가 머문다. 가는 도중에 다시 부르면 목표만 바뀐다.</summary>
        public void MoveTo(Vector2 worldPoint)
        {
            target = worldPoint;
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

        private Vector2 ScreenToWorld(Vector2 screen) =>
            worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
    }
}
