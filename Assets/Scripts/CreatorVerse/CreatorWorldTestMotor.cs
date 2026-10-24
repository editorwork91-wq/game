using UnityEngine;

namespace CreatorVerse
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CreatorWorldTestMotor : MonoBehaviour
    {
        private CharacterController controller;
        private Transform viewCamera;
        private Vector3 vertical;

        public void Configure(Camera cameraTarget)
        {
            controller = GetComponent<CharacterController>();
            viewCamera = cameraTarget ? cameraTarget.transform : null;
        }

        private void Awake() { controller = GetComponent<CharacterController>(); }

        private void Update()
        {
            if (!controller) controller = GetComponent<CharacterController>();
            Vector2 input = Vector2.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
            Vector3 forward = viewCamera ? Vector3.ProjectOnPlane(viewCamera.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = viewCamera ? Vector3.ProjectOnPlane(viewCamera.right, Vector3.up).normalized : Vector3.right;
            Vector3 direction = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);

            controller.Move(direction * 5.2f * Time.deltaTime);
            if (controller.isGrounded && vertical.y < 0f) vertical.y = -2f;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetButtonDown("Jump") && controller.isGrounded) vertical.y = 6.2f;
#endif
            vertical.y += -22f * Time.deltaTime;
            controller.Move(vertical * Time.deltaTime);
            if (viewCamera)
            {
                viewCamera.position = Vector3.Lerp(viewCamera.position, transform.position + new Vector3(0f, 8f, -10f), 7f * Time.deltaTime);
                viewCamera.LookAt(transform.position + Vector3.up);
            }
        }
    }
}