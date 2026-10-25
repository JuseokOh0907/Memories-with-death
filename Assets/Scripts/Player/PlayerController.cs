using UnityEngine;

namespace MetroidVania
{
    public enum PlayerState
    {
        Grounded,
        Rising,
        Falling,
        Dashing,    // 4일차
        Hurt,       // 3일차
        Dead,       // 3일차
    }

    /// <summary>
    /// 이동 상태기계. 입력과 모터의 감지 결과로 상태를 정하고 모터에 명령한다.
    /// 공격은 상태가 아니다 — 공중 이동·중력을 유지한 채 겹쳐 돌아가므로 PlayerCombat 이 따로 맡는다.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerTuning tuning;
        [Tooltip("좌우 반전할 표시 오브젝트. 충돌체는 반전하지 않는다")]
        [SerializeField] private Transform visual;

        private PlayerMotor motor;
        private PlayerInputReader input;

        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool jumpCut;

        public PlayerState State { get; private set; } = PlayerState.Falling;
        public int Facing { get; private set; } = 1;
        public PlayerMotor Motor => motor;
        public float CoyoteTimer => coyoteTimer;
        public float JumpBufferTimer => jumpBufferTimer;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            input = GetComponent<PlayerInputReader>();

            if (tuning == null)
            {
                Debug.LogError($"{name}: PlayerTuning 이 비어 있다.", this);
                enabled = false;
                return;
            }
            motor.Initialize(tuning);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            motor.Sense();
            TickTimers(dt);
            UpdateFacing();

            switch (State)
            {
                case PlayerState.Grounded: TickGrounded(dt); break;
                case PlayerState.Rising: TickRising(dt); break;
                case PlayerState.Falling: TickFalling(dt); break;
            }

            motor.Commit();
        }

        private void TickTimers(float dt)
        {
            coyoteTimer = motor.IsGrounded ? tuning.coyoteTime : coyoteTimer - dt;

            // 버퍼는 "누른 순간"에만 채운다. 누르고 있는 것만으로는 다시 뛰지 않는다.
            jumpBufferTimer = input.ConsumeJumpPressed() ? tuning.jumpBufferTime : jumpBufferTimer - dt;
        }

        private void TickGrounded(float dt)
        {
            motor.Run(input.MoveX, tuning.groundAcceleration, tuning.groundDeceleration, dt);
            // 지상에서도 중력을 건다. 바닥이 받쳐 주므로 속도는 솔버가 0 으로 되돌린다.
            motor.ApplyGravity(tuning.Gravity, dt);

            if (TryJump())
                return;
            if (!motor.IsGrounded)
                State = PlayerState.Falling;    // 코요테 타이머는 계속 흐른다
        }

        private void TickRising(float dt)
        {
            motor.Run(input.MoveX, tuning.airAcceleration, tuning.airDeceleration, dt);

            if (!jumpCut && !input.JumpHeld)
            {
                motor.MultiplyVerticalVelocity(tuning.jumpCutFactor);
                jumpCut = true;
            }

            motor.ApplyGravity(tuning.Gravity, dt);

            if (motor.HitCeiling)
                motor.SetVerticalVelocity(0f);
            if (motor.Velocity.y <= 0f)
                State = PlayerState.Falling;
        }

        private void TickFalling(float dt)
        {
            motor.Run(input.MoveX, tuning.airAcceleration, tuning.airDeceleration, dt);

            if (TryJump())
                return;

            motor.ApplyGravity(tuning.Gravity * tuning.fallGravityMultiplier, dt);

            if (motor.IsGrounded)
            {
                State = PlayerState.Grounded;
                TryJump();  // 착지 직전에 누른 점프를 이 스텝에서 바로 쓴다
            }
        }

        private bool TryJump()
        {
            if (jumpBufferTimer <= 0f || coyoteTimer <= 0f)
                return false;

            motor.SetVerticalVelocity(tuning.JumpVelocity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            jumpCut = false;
            State = PlayerState.Rising;
            return true;
        }

        private void UpdateFacing()
        {
            if (input.MoveX > 0f) Facing = 1;
            else if (input.MoveX < 0f) Facing = -1;

            if (visual != null)
            {
                Vector3 scale = visual.localScale;
                scale.x = Mathf.Abs(scale.x) * Facing;
                visual.localScale = scale;
            }
        }
    }
}
