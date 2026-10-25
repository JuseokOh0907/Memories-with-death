using UnityEngine;

namespace MetroidVania
{
    /// <summary>
    /// 조작감 수치를 한곳에 모은다. 단위는 전부 월드 유닛·초.
    /// 아트를 바꿔도 이 값은 그대로다 — 판정은 충돌체 기준이다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerTuning", menuName = "MetroidVania/Player Tuning")]
    public class PlayerTuning : ScriptableObject
    {
        [Header("수평 이동")]
        [Min(0f)] public float maxRunSpeed = 8f;
        [Tooltip("지상에서 입력 방향으로 붙는 가속 (유닛/초²). 80 이면 0.1초에 최고 속도")]
        [Min(0f)] public float groundAcceleration = 80f;
        [Tooltip("지상에서 손을 뗐을 때 감속. 100 이면 0.08초에 정지")]
        [Min(0f)] public float groundDeceleration = 100f;
        [Min(0f)] public float airAcceleration = 50f;
        [Min(0f)] public float airDeceleration = 25f;

        [Header("점프 — 높이와 정점까지 시간으로 정한다")]
        [Tooltip("버튼을 끝까지 누른 점프의 최고 높이 (유닛)")]
        [Min(0.1f)] public float jumpHeight = 3.5f;
        [Tooltip("정점까지 걸리는 시간 (초). 중력과 초기 속도가 여기서 계산된다")]
        [Min(0.05f)] public float timeToApex = 0.4f;
        [Tooltip("하강 중 중력 배율. 1 보다 크면 내려올 때 더 빠르다 (임시값 — 하강 중력 자료를 받아 교체)")]
        [Min(1f)] public float fallGravityMultiplier = 1.5f;

        [Header("정점 — 공중 공격 지점도 여기에 들어온다")]
        [Tooltip("수직 속도의 크기가 이보다 작으면 정점 구간으로 본다 (유닛/초). 0 이면 끈다")]
        [Min(0f)] public float apexSpeedThreshold = 2f;
        [Tooltip("정점 구간의 중력 배율. 1 보다 작으면 최고점에서 잠깐 머문다. 하강 배율보다 우선한다")]
        [Range(0f, 1f)] public float apexGravityMultiplier = 0.5f;
        [Tooltip("상승 중 버튼을 떼면 남은 상승 속도에 곱한다. 작을수록 짧게 끊긴다")]
        [Range(0f, 1f)] public float jumpCutFactor = 0.45f;
        [Min(0f)] public float maxFallSpeed = 20f;

        [Header("입력 보정")]
        [Tooltip("발판을 벗어난 뒤에도 점프를 허용하는 시간")]
        [Min(0f)] public float coyoteTime = 0.1f;
        [Tooltip("착지 전에 누른 점프를 기억하는 시간")]
        [Min(0f)] public float jumpBufferTime = 0.1f;

        [Header("충돌 감지")]
        [Tooltip("바닥·천장·벽을 찾는 캐스트 거리. Default Contact Offset(0.01)보다 커야 한다")]
        [Min(0.01f)] public float probeDistance = 0.05f;
        [Tooltip("감지 방향을 이 값 이상 마주 보는 면만 바닥·벽·천장으로 본다 (0.7 ≈ 45°)")]
        [Range(0f, 1f)] public float minGroundNormalY = 0.7f;

        /// <summary>g = 2h / t²</summary>
        public float Gravity => 2f * jumpHeight / (timeToApex * timeToApex);

        /// <summary>v₀ = g·t</summary>
        public float JumpVelocity => Gravity * timeToApex;
    }
}
