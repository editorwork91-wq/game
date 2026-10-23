using UnityEngine;

namespace LastLift
{
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 5.4f, -7.3f);
        [SerializeField] private float smoothing = 8f;

        public void SetTarget(Transform value) => target = value;

        private void LateUpdate()
        {
            if (!target) return;
            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothing * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * 1.05f);
        }
    }
}