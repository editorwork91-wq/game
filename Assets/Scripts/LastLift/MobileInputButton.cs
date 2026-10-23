using UnityEngine;
using UnityEngine.EventSystems;

namespace LastLift
{
    public sealed class MobileInputButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum ActionType { Move, Jump }
        [SerializeField] private ActionType action;
        [SerializeField] private Vector2 direction;
        private PlayerMotor motor;

        public void Setup(PlayerMotor target, ActionType kind, Vector2 moveDirection)
        {
            motor = target;
            action = kind;
            direction = moveDirection;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!motor) return;
            if (action == ActionType.Jump) motor.QueueJump();
            else motor.SetMobileInput(direction);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (motor && action == ActionType.Move) motor.SetMobileInput(Vector2.zero);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (motor && action == ActionType.Move) motor.SetMobileInput(Vector2.zero);
        }
    }
}