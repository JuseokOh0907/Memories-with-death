using UnityEngine;

namespace MetroidVania
{
    /// <summary>
    /// 물리 실행만 맡는다: 접지·천장·벽 감지와 속도 적용. 상태 판단은 하지 않는다.
    /// 자체 FixedUpdate 가 없고 PlayerController 가 Sense → 명령 → Commit 순서로 부른다.
    /// 한 물리 스텝 안에서 순서가 고정돼야 접지 판정이 흔들리지 않는다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMotor : MonoBehaviour
    {
        [Tooltip("바닥·벽·천장으로 볼 레이어")]
        [SerializeField] private LayerMask solidLayers = ~0;

        private Rigidbody2D body;
        private PlayerTuning tuning;
        private ContactFilter2D solidFilter;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];

        private Vector2 velocity;

        public bool IsGrounded { get; private set; }
        public bool HitCeiling { get; private set; }
        public bool WallLeft { get; private set; }
        public bool WallRight { get; private set; }
        public Vector2 Velocity => velocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            // 인스펙터 값과 상관없이 이 구조가 전제하는 설정으로 고정한다.
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;                       // 중력은 ApplyGravity 가 계산한다
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 빠른 이동 관통 방지

            solidFilter = new ContactFilter2D { useTriggers = false };
            solidFilter.SetLayerMask(solidLayers);
        }

        public void Initialize(PlayerTuning playerTuning) => tuning = playerTuning;

        /// <summary>물리 스텝 시작에 부른다. 충돌로 바뀐 속도를 다시 읽고 주변을 감지한다.</summary>
        public void Sense()
        {
            velocity = body.linearVelocity;

            // 상승 중에는 바닥을 보지 않는다 — 발판 모서리를 스치며 올라갈 때 접지로 잡히는 것을 막는다.
            IsGrounded = velocity.y <= 0.01f && Probe(Vector2.down);
            HitCeiling = velocity.y > 0f && Probe(Vector2.up);
            WallLeft = Probe(Vector2.left);
            WallRight = Probe(Vector2.right);
        }

        public void Run(float input, float acceleration, float deceleration, float dt)
        {
            float target = input * tuning.maxRunSpeed;
            float rate = Mathf.Approximately(input, 0f) ? deceleration : acceleration;
            velocity.x = Mathf.MoveTowards(velocity.x, target, rate * dt);

            // 벽으로 미는 속도를 남겨두면 모서리에 끼거나 떨린다.
            if (WallRight && velocity.x > 0f) velocity.x = 0f;
            if (WallLeft && velocity.x < 0f) velocity.x = 0f;
        }

        public void ApplyGravity(float gravity, float dt)
        {
            velocity.y = Mathf.Max(velocity.y - gravity * dt, -tuning.maxFallSpeed);
        }

        public void SetVerticalVelocity(float value) => velocity.y = value;

        public void MultiplyVerticalVelocity(float factor) => velocity.y *= factor;

        /// <summary>물리 스텝 끝에 부른다.</summary>
        public void Commit() => body.linearVelocity = velocity;

        /// <summary>
        /// direction 쪽에 이쪽을 마주 보는 면이 있는가. 바닥이면 법선이 위를,
        /// 벽이면 옆을 향해야 한다 — 모서리의 비스듬한 면은 어느 쪽으로도 세지 않는다.
        /// </summary>
        private bool Probe(Vector2 direction)
        {
            int count = body.Cast(direction, solidFilter, hits, tuning.probeDistance);
            for (int i = 0; i < count; i++)
            {
                if (Vector2.Dot(hits[i].normal, -direction) >= tuning.minGroundNormalY)
                    return true;
            }
            return false;
        }
    }
}
