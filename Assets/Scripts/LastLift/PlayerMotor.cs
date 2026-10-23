using UnityEngine;

namespace LastLift
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float jumpHeight = 2.05f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private Transform cameraTransform;

        private CharacterController controller;
        private Vector3 verticalVelocity;
        private Vector2 mobileInput;
        private bool jumpQueued;
        private bool canControl = true;
        private Renderer[] visualRenderers;

        public bool CanControl { get => canControl; set => canControl = value; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
            CacheVisuals();
        }

        public void SetCamera(Transform target) => cameraTransform = target;
        public void SetMobileInput(Vector2 value) => mobileInput = Vector2.ClampMagnitude(value, 1f);
        public void QueueJump() => jumpQueued = true;

        public void ApplySkin(int skinIndex)
        {
            CacheVisuals();
            Color color;
            switch (Mathf.Abs(skinIndex) % 4)
            {
                case 1: color = new Color(1f, 0.43f, 0.25f); break;
                case 2: color = new Color(0.63f, 0.42f, 1f); break;
                case 3: color = new Color(0.28f, 0.88f, 0.62f); break;
                default: color = new Color(0.2f, 0.66f, 1f); break;
            }

            foreach (Renderer item in visualRenderers)
            {
                if (!item) continue;
                foreach (Material material in item.materials)
                {
                    if (material && material.HasProperty("_Color")) material.color = color;
                    if (material && material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                }
            }
        }

        private void CacheVisuals() => visualRenderers = GetComponentsInChildren<Renderer>(true);

        private void Update()
        {
            if (!canControl) return;

            Vector2 input = mobileInput;
#if ENABLE_LEGACY_INPUT_MANAGER
            Vector2 keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (keyboard.sqrMagnitude > input.sqrMagnitude) input = keyboard;
            if (Input.GetButtonDown("Jump")) jumpQueued = true;
#endif
            Vector3 forward = cameraTransform
                ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
                : Vector3.forward;
            Vector3 right = cameraTransform
                ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized
                : Vector3.right;
            Vector3 direction = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);

            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.deltaTime);

            controller.Move(direction * moveSpeed * Time.deltaTime);
            if (controller.isGrounded && verticalVelocity.y < 0f) verticalVelocity.y = -2f;
            if (jumpQueued && controller.isGrounded) verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpQueued = false;
            verticalVelocity.y += gravity * Time.deltaTime;
            controller.Move(verticalVelocity * Time.deltaTime);

            if (transform.position.y < -8f && GameFlow.Instance) GameFlow.Instance.Fail();
        }
    }
}