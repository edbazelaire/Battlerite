using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects
{
    public class NetworkManaState: NetworkBehaviour
    {
        // [HideInInspector]
        public NetworkVariable<float> ManaReserve = new ();

        // [HideInInspector]
        public NetworkVariable<float> Mana = new ();

        [HideInInspector]
        public float MaxManaReserve;
        [HideInInspector]
        public float MaxManaPool;
    }
}