using UnityEngine;

namespace LastLift
{
    public sealed class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private float verticalAmplitude = 1.1f;
        [SerializeField] private float speed = 1.0f;
        private Vector3 startPosition;

        private void Start() => startPosition = transform.position;

        private void Update()
        {
            Vector3 next = startPosition;
            next.y += (Mathf.Sin(Time.time * speed) + 1f) * 0.5f * verticalAmplitude;
            transform.position = next;
        }
    }
}