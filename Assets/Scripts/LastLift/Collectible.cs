using UnityEngine;

namespace LastLift
{
    public sealed class Collectible : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        private bool collected;

        private void Update() => transform.Rotate(0f, 110f * Time.deltaTime, 0f, Space.World);

        private void OnTriggerEnter(Collider other)
        {
            if (collected || !other.GetComponentInParent<PlayerMotor>()) return;
            collected = true;
            LocalProfileService.AddCoins(value);
            if (GameFlow.Instance) GameFlow.Instance.RefreshHUD();
            Destroy(gameObject);
        }
    }
}