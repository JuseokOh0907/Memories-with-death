using UnityEngine;

namespace MetroidVania
{
    /// <summary>
    /// 플레이어를 부드럽게 따라가되 맵 경계 밖은 보여주지 않는다.
    /// 수평은 바라보는 쪽으로 앞 공간을 더 보여주고,
    /// 수직은 착지했을 때나 데드존을 벗어났을 때만 따라가 작은 점프에 화면이 출렁이지 않는다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private PlayerController target;
        [Tooltip("비워 두면 경계 없이 따라간다. Is Trigger 를 켠 BoxCollider2D 로 맵 범위를 그린다")]
        [SerializeField] private BoxCollider2D bounds;

        [Header("구도")]
        [Tooltip("카메라 중심이 플레이어 발보다 얼마나 위에 있나. 양수면 플레이어가 화면 중앙보다 아래에 선다")]
        [SerializeField] private float verticalOffset = 1f;
        [SerializeField] private float lookAhead = 2.5f;

        [Header("추적")]
        [SerializeField, Min(0f)] private float horizontalSmoothTime = 0.15f;
        [SerializeField, Min(0f)] private float lookAheadSmoothTime = 0.5f;
        [SerializeField, Min(0f)] private float verticalSmoothTime = 0.25f;
        [Tooltip("공중에서 기준선보다 이만큼 올라가기 전에는 따라 올라가지 않는다 — 점프마다 출렁이지 않게")]
        [SerializeField, Min(0f)] private float deadZoneUp = 2f;
        [Tooltip("기준선보다 이만큼 내려가면 따라 내려간다. 작게 둬야 떨어질 때 착지할 바닥이 보인다")]
        [SerializeField, Min(0f)] private float deadZoneDown = 0.5f;

        private Camera cam;
        private float currentLookAhead;
        private float lookAheadVelocity;
        private float baseY;    // 수직 추적 기준선 (플레이어 발 높이)
        private Vector2 smoothVelocity;

        private void Awake() => cam = GetComponent<Camera>();

        private void Start() => SnapToTarget();

        private void LateUpdate()
        {
            if (target == null)
                return;

            float dt = Time.deltaTime;
            Vector3 targetPos = target.transform.position;

            currentLookAhead = Mathf.SmoothDamp(currentLookAhead, target.Facing * lookAhead,
                ref lookAheadVelocity, lookAheadSmoothTime, Mathf.Infinity, dt);

            if (target.Motor.IsGrounded)
                baseY = targetPos.y;
            else if (targetPos.y > baseY + deadZoneUp)
                baseY = targetPos.y - deadZoneUp;
            else if (targetPos.y < baseY - deadZoneDown)
                baseY = targetPos.y + deadZoneDown;

            Vector2 desired = Clamp(new Vector2(targetPos.x + currentLookAhead, baseY + verticalOffset));
            Vector3 pos = transform.position;
            pos.x = Mathf.SmoothDamp(pos.x, desired.x, ref smoothVelocity.x, horizontalSmoothTime, Mathf.Infinity, dt);
            pos.y = Mathf.SmoothDamp(pos.y, desired.y, ref smoothVelocity.y, verticalSmoothTime, Mathf.Infinity, dt);
            transform.position = pos;
        }

        /// <summary>리스폰처럼 순간 이동했을 때 부른다. 사망 위치에서 날아오지 않고 바로 맞춘다.</summary>
        public void SnapToTarget()
        {
            if (target == null)
                return;

            Vector3 targetPos = target.transform.position;
            baseY = targetPos.y;
            currentLookAhead = target.Facing * lookAhead;
            lookAheadVelocity = 0f;
            smoothVelocity = Vector2.zero;

            Vector2 desired = Clamp(new Vector2(targetPos.x + currentLookAhead, baseY + verticalOffset));
            transform.position = new Vector3(desired.x, desired.y, transform.position.z);
        }

        private Vector2 Clamp(Vector2 center)
        {
            if (bounds == null)
                return center;

            Bounds b = bounds.bounds;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            // 맵이 화면보다 좁으면 가운데에 고정한다.
            center.x = b.size.x <= halfW * 2f ? b.center.x : Mathf.Clamp(center.x, b.min.x + halfW, b.max.x - halfW);
            center.y = b.size.y <= halfH * 2f ? b.center.y : Mathf.Clamp(center.y, b.min.y + halfH, b.max.y - halfH);
            return center;
        }
    }
}
