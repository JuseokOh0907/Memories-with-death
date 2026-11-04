using UnityEngine;

namespace MetroidVania
{
    /// <summary>
    /// 방 하나의 범위. 트리거 박스가 곧 카메라 경계이고, 플레이어가 들어오면 현재 방이 된다.
    /// 충돌은 여기서 다루지 않는다 — 지형은 Level/Solid 타일맵 하나에 모여 있다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Room : MonoBehaviour
    {
        [Tooltip("체크포인트·디버그 표시에 쓰는 이름. 비우면 오브젝트 이름")]
        [SerializeField] private string roomId;
        [Tooltip("이 방에서 낙사했을 때 돌아올 위치 (3일차에 사용). 플레이어 발이 놓일 자리")]
        [SerializeField] private Transform safePoint;

        private BoxCollider2D area;

        public string Id => string.IsNullOrEmpty(roomId) ? name : roomId;
        public Transform SafePoint => safePoint;
        public BoxCollider2D Area => area;

        private void Awake()
        {
            area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (TryGetTracker(other, out PlayerRoomTracker tracker))
                tracker.NotifyEnter(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (TryGetTracker(other, out PlayerRoomTracker tracker))
                tracker.NotifyExit(this);
        }

        private static bool TryGetTracker(Collider2D other, out PlayerRoomTracker tracker)
        {
            tracker = null;
            Rigidbody2D body = other.attachedRigidbody;
            return body != null && body.TryGetComponent(out tracker);
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null)
                return;

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
            Gizmos.matrix = Matrix4x4.identity;

            if (safePoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(safePoint.position, 0.3f);
            }
        }
    }
}
