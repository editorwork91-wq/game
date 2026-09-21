using UnityEngine;

namespace Yomy.Character
{
    // Safe vertical physics helper. It does not move horizontally, rotate,
    // or modify the mesh, skeleton, materials, or animation clips.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class YomyCharacterGravity : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float gravityScale = 1f;

        [SerializeField]
        private float groundedStickVelocity = -2f;

        [SerializeField]
        private bool applyGravity = true;

        private CharacterController controller;

        public bool IsGrounded => controller != null && controller.isGrounded;
        public float VerticalVelocity { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!applyGravity || controller == null || !controller.enabled)
                return;

            if (controller.isGrounded && VerticalVelocity < 0f)
                VerticalVelocity = groundedStickVelocity;

            VerticalVelocity += Physics.gravity.y * gravityScale * Time.deltaTime;
            controller.Move(Vector3.up * (VerticalVelocity * Time.deltaTime));
        }

        public void SetVerticalVelocity(float velocity)
        {
            VerticalVelocity = velocity;
        }

        public void AddVerticalImpulse(float jumpSpeed)
        {
            VerticalVelocity = Mathf.Max(VerticalVelocity, jumpSpeed);
        }

        public void SetGravityEnabled(bool enabled)
        {
            applyGravity = enabled;
        }
    }
}
