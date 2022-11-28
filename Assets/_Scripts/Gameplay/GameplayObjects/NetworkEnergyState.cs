using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects
{
    public class NetworkEnergyState: NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<int> Energy = new NetworkVariable<int>();

        public const int MAX_ENERGY = 100;
    }
}