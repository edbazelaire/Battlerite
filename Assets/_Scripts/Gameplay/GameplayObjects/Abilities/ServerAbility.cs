using _Scripts.Gameplay.Configuration;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects
{
    public class ServerAbility: NetworkBehaviour
    {
        protected ulong _spawnerId;
        public ulong SpawnerId => _spawnerId;
        
        protected bool _isStarted;
        protected ActionDescription Description;

        /// <summary>
        /// is ability done ?
        /// </summary>
        protected bool _isDead;

        public void Initialize(ulong spawnerId, ActionDescription description)
        {
            _spawnerId = spawnerId;
            Description = description;
        }
        
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }
            
            _isStarted = true;
        }

        public override void OnNetworkDespawn()
        {
            Destroy(gameObject);
        }
        
        
        protected Vector3 GetFinalPosition(Vector3 initialPosition, Vector3 mousePosition)
        {
            var finalDistance = GetFinalRange(initialPosition, mousePosition);

            var distance = Vector3.Distance(initialPosition, mousePosition);
            var finalPosition = initialPosition + (mousePosition - initialPosition) * finalDistance / distance;
            finalPosition.y = transform.position.y;
            
            return finalPosition;
        }

        protected float GetFinalRange(Vector3 initialPosition, Vector3 mousePosition)
        {
            if (! Description.IsOnMousePosition)
            {
                return Description.MaxRange;
            }
            
            var distance = Vector3.Distance(initialPosition, mousePosition);
            return Mathf.Clamp(distance, Description.MinRange, Description.MaxRange);
        }
        
        public virtual void Kill()
        {
            if (_isDead) { return; }

            _isDead = true;
            var networkObject = gameObject.GetComponent<NetworkObject>();
            networkObject.Despawn();
            Destroy(gameObject);
        }
    }
}