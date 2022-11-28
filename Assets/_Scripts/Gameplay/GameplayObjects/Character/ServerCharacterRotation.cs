using System;
using _Scripts.Gameplay.GameplayObjects.Abilities;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects.Character
{
    public enum RotationState
    {
        Normal,     // player can rotate normally
        Cancel,     // player can't rotate
        Spin        // player is forced to make a spinning move
    }
    
    /// <summary>
    /// Component responsible for rotating a character on the server side based on inputs.
    /// </summary>
    public class ServerCharacterRotation: NetworkBehaviour
    {
        [SerializeField]
        private NetworkCharacterState m_NetworkCharacterState;
        
        private RotationState _rotationState;
        private Quaternion _rotation;
        private Quaternion _previousRotation;

        void Awake()
        {
            // disable this NetworkBehavior until it is spawned
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Only enable server component on servers
                enabled = true;
            }
        }
        
        private void FixedUpdate()
        {
            PerformRotation();

            m_NetworkCharacterState.Rotation.Value = transform.rotation;
        }

        private void PerformRotation() {
            switch (_rotationState)
            {
                case(RotationState.Normal):
                case(RotationState.Cancel):
                    // can not rotate on X, Y during normal mode 
                    _rotation.x = 0;
                    _rotation.z = 0;
                    transform.rotation = _rotation;
                    return;
                    
                case(RotationState.Spin):
                    throw new NotImplementedException();
                    
                default:
                    throw new NotImplementedException();
            }
        }

        public void CancelRotation()
        {
            _rotationState = RotationState.Cancel;
        }

        public void EnableRotation()
        {
            if (_rotationState is RotationState.Cancel)
            {
                _rotationState = RotationState.Normal;
            } 
        }
        
        
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                enabled = false;
            }
        }
        
        /// <summary>
        /// Sets a movement target. We will path to this position, avoiding static obstacles.
        /// </summary>
        /// <param name="direction">Direction to follow</param>
        public void SetRotation(Quaternion rotation)
        {
            if (! CanRotate()) { return; }    
            _rotation = rotation;
        }
        
        /// <summary>
        /// Returns true if the current movement-mode is unabortable (e.g. a knockback effect)
        /// </summary>
        /// <returns></returns>
        public bool IsPerformingForcedRotation()
        {
            return _rotationState is RotationState.Cancel 
                or RotationState.Spin;
        }
        
        /// <summary>
        /// Return true if the character is not prevented to input a rotation (already performing a forced rotation or
        /// being incapacitated by a spell effect)
        /// </summary>
        /// <returns></returns>
        private bool CanRotate()
        {
            return ! IsPerformingForcedRotation()
                   && m_NetworkCharacterState.NetworkControlState.StateEffectType.Value != StateEffectType.Stun
                   && m_NetworkCharacterState.NetworkControlState.StateEffectType.Value != StateEffectType.AirBorn
                   && m_NetworkCharacterState.NetworkControlState.StateEffectType.Value != StateEffectType.Petrify;
        }
    }
}