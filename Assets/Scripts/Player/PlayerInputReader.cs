using UnityEngine;
using UnityEngine.InputSystem;

namespace MetroidVania
{
    /// <summary>
    /// 프로젝트 전역 입력 액션(InputSystem.actions)을 읽어 값으로 바꾼다.
    /// 누름은 Update 에서 플래그로 쌓고 FixedUpdate 쪽이 Consume 으로 한 번만 가져간다 —
    /// 프레임 수가 달라도 누름이 빠지거나 두 번 처리되지 않는다.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Tooltip("스틱 입력이 이보다 작으면 0 으로 본다")]
        [SerializeField, Range(0f, 1f)] private float moveDeadZone = 0.25f;

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction attackAction;
        private InputAction dashAction;
        private InputAction interactAction;

        private bool jumpPressed;
        private bool attackPressed;
        private bool dashPressed;
        private bool interactPressed;

        private bool blocked;
        private bool waitForRelease;

        public float MoveX { get; private set; }
        public bool JumpHeld { get; private set; }

        private void Awake()
        {
            InputActionAsset actions = InputSystem.actions;
            moveAction = actions.FindAction("Player/Move", true);
            jumpAction = actions.FindAction("Player/Jump", true);
            attackAction = actions.FindAction("Player/Attack", true);
            dashAction = actions.FindAction("Player/Dash", true);
            interactAction = actions.FindAction("Player/Interact", true);
        }

        private void Update()
        {
            if (blocked)
                return;

            float x = moveAction.ReadValue<Vector2>().x;
            MoveX = Mathf.Abs(x) < moveDeadZone ? 0f : Mathf.Clamp(x, -1f, 1f);

            // 창을 닫을 때 누르고 있던 버튼이 게임으로 새지 않게, 전부 뗄 때까지 누름을 받지 않는다.
            if (waitForRelease)
            {
                if (jumpAction.IsPressed() || attackAction.IsPressed() || dashAction.IsPressed())
                    return;
                waitForRelease = false;
            }

            JumpHeld = jumpAction.IsPressed();
            if (jumpAction.WasPressedThisFrame()) jumpPressed = true;
            if (attackAction.WasPressedThisFrame()) attackPressed = true;
            if (dashAction.WasPressedThisFrame()) dashPressed = true;
            if (interactAction.WasPressedThisFrame()) interactPressed = true;
        }

        public bool ConsumeJumpPressed() => Consume(ref jumpPressed);
        public bool ConsumeAttackPressed() => Consume(ref attackPressed);
        public bool ConsumeDashPressed() => Consume(ref dashPressed);
        public bool ConsumeInteractPressed() => Consume(ref interactPressed);

        /// <summary>UI 창이 열리고 닫힐 때 부른다. 막는 동안의 입력은 버려진다.</summary>
        public void SetBlocked(bool value)
        {
            blocked = value;
            MoveX = 0f;
            JumpHeld = false;
            jumpPressed = attackPressed = dashPressed = interactPressed = false;
            if (!value)
                waitForRelease = true;
        }

        private static bool Consume(ref bool flag)
        {
            bool value = flag;
            flag = false;
            return value;
        }
    }
}
