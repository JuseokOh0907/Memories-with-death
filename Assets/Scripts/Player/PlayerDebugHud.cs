using UnityEngine;

namespace MetroidVania
{
    /// <summary>
    /// 개발용 상태 표시. 정상 플레이에서는 컴포넌트를 끄거나 Show 를 끈다.
    /// 체력·대시·체크포인트 줄은 해당 기능이 생길 때 덧붙인다.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerDebugHud : MonoBehaviour
    {
        [SerializeField] private bool show = true;

        private PlayerController controller;
        private GUIStyle style;

        private void Awake() => controller = GetComponent<PlayerController>();

        private void OnGUI()
        {
            if (!show)
                return;

            style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 16 };

            PlayerMotor motor = controller.Motor;
            Vector2 v = motor.Velocity;
            string text =
                $"State    {controller.State}\n" +
                $"Grounded {motor.IsGrounded}   Ceiling {motor.HitCeiling}\n" +
                $"Wall     L {motor.WallLeft}  R {motor.WallRight}\n" +
                $"Velocity ({v.x:0.00}, {v.y:0.00})\n" +
                $"Facing   {controller.Facing}\n" +
                $"Coyote   {Mathf.Max(0f, controller.CoyoteTimer):0.000}\n" +
                $"Buffer   {Mathf.Max(0f, controller.JumpBufferTimer):0.000}";

            GUI.Box(new Rect(Screen.width - 330, 10, 320, 170), text, style);
        }
    }
}
