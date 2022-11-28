using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects.Abilities
{
    public class ServerProjectileLogic : ServerHitAbilityLogic
    {
        private float _currentSpeed => Description.Speed * _speedFactor;
        private float _speedFactor = 1;
        
        protected float _range;
        protected Vector3 _initialPosition;
        
        public void Initialize(ulong spawnerId, ActionDescription description, float finalRange)
        {
            base.Initialize(spawnerId, description);
            
            _range = finalRange;
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            _initialPosition = transform.position;
        }

        protected override void FixedUpdate()
        {
            if (!_isStarted)
            {
                return; //don't do anything before OnNetworkSpawn has run.
            }
            
            var displacement = transform.forward * (_currentSpeed * Time.fixedDeltaTime);
            transform.position += displacement;

            // check 2D distance travelled
            var distance = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(_initialPosition.x, _initialPosition.z)
            );

            if (distance >= _range)
            {
                Debug.Log("Max distance reached");
                
                // if Radius provided : create AOE on MaxRange
                if (Description.Radius > 0)
                {
                    DetectCollisions(transform.position, Description.Radius, true);
                }
                
                Kill();
                return;
            }

            // if no Radius provided, simple projectile collision logic
            if (Description.Radius == 0)
            {
                DetectCollisions();
            }
        }
    }
}

