using _Scripts.Gameplay.GameplayObjects.Abilities;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects
{
    public class NetworkControlState: NetworkBehaviour
    {
        [SerializeField] 
        NetworkVariable<StateEffectType> m_StateEffectType = new(Abilities.StateEffectType.Normal);
        
        public NetworkVariable<StateEffectType> StateEffectType => m_StateEffectType;

    }
}