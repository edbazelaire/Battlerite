using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects
{
    /// <summary>
    /// MonoBehaviour containing only one NetworkVariableInt which represents this object's health.
    /// </summary>
    public class NetworkHealthState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<int> HitPoints = new ();

        [HideInInspector]
        public NetworkVariable<int> Shield = new ();

        // public subscribable event to be invoked when HP has been fully depleted
        public event System.Action hitPointsDepleted;

        // public subscribable event to be invoked when HP has been replenished
        public event System.Action hitPointsReplenished;

        void OnEnable()
        {
            HitPoints.OnValueChanged += HitPointsChanged;
        }

        void OnDisable()
        {
            HitPoints.OnValueChanged -= HitPointsChanged;
        }

        void HitPointsChanged(int previousValue, int newValue)
        {
            if (previousValue > 0 && newValue <= 0)
            {
                // newly reached 0 HP
                hitPointsDepleted?.Invoke();
            }
            else if (previousValue <= 0 && newValue > 0)
            {
                // newly revived
                hitPointsReplenished?.Invoke();
            }
        }
    }
}
