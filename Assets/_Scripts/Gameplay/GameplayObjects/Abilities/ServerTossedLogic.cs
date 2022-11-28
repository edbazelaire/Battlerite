using _Scripts.Gameplay.Configuration;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.Gameplay.GameplayObjects.Abilities
{
    public class ServerTossedLogic : ServerProjectileLogic
    {
        // final position where the projectile ends
        private Vector3 _finalPosition;
        
        // position in the air use to control position and direction of the projectile 
        private Vector3 _controlPoint;
        
        // percentage of the distance travelled by the projectile
        private float _progression;
        
        public UnityEvent detonatedCallback;

        public void Initialize(ulong creatorsNetworkObjectId, ActionDescription description, Vector3 finalPosition)
        {
            _spawnerId = creatorsNetworkObjectId;
            Description = description;   
            
            _finalPosition = finalPosition;
            _finalPosition.y = 0;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            base.OnNetworkSpawn();
            
            _controlPoint = _initialPosition + (_finalPosition -_initialPosition) / 2 + Vector3.up * 10.0f; 
        }

        [ClientRpc]
        void DetonateClientRpc()
        {
            detonatedCallback?.Invoke();
        }

        protected override void FixedUpdate()
        {
            if (!_isStarted)
            {
                return; //don't do anything before OnNetworkSpawn has run.
            }

            UpdatePosition();
            
            if (transform.position == _finalPosition)
            {
                // detect collision on damageable in the radius area
                DetectCollisions(_finalPosition, Description.Radius, true);
                
                // alert client that detonation has been done
                DetonateClientRpc();
                
                // destroy and despawn the object
                Kill();
            }
        }

        protected void UpdatePosition()
        {
            _progression += 1.0f * Time.fixedDeltaTime / Description.ActivationDelay;

            Vector3 m1 = Vector3.Lerp( _initialPosition, _controlPoint, _progression );
            Vector3 m2 = Vector3.Lerp( _controlPoint, _finalPosition, _progression );
            
            // calculate rotation according to y direction
            var dirY = (m2 - m1).normalized.y;              
            var finalRotation = transform.eulerAngles;
            finalRotation.y = - dirY * 90;
            
            // set position and rotation of the object
            transform.position = Vector3.Lerp(m1, m2, _progression);
            transform.eulerAngles = finalRotation;
        }
    }
}

