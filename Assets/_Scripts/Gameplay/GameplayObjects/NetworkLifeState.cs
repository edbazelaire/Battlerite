using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects
{
    /// <summary>
    /// NetworkBehaviour containing only one NetworkVariable of type LifeState which represents this object's life state.
    /// </summary>
    public class NetworkLifeState : NetworkBehaviour
    {
        [SerializeField]
        NetworkVariable<LifeState> m_LifeState = new(GameplayObjects.LifeState.Alive);

        public NetworkVariable<LifeState> LifeState => m_LifeState;
    }
}
