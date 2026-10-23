using UnityEngine;

namespace LastLift
{
    public sealed class LevelFinish : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<PlayerMotor>()) return;
            if (GameFlow.Instance) GameFlow.Instance.Win();
        }
    }
}