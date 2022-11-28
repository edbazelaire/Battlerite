using _Scripts.Gameplay.Configuration;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects.Abilities
{
    public class ServerAoeLogic: ServerHitAbilityLogic
    {
        public void Initialize(ulong spawnerId, ActionDescription description, Vector3 position)
        {
            base.Initialize(spawnerId, description);
            transform.position = position;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            base.OnNetworkSpawn();

            _destroyAtSec = Time.time + Description.ActivationDelay;
            transform.localScale = Vector3.one * (Description.Radius / m_OurCollider.radius);
        }

        protected override void FixedUpdate()
        {
            if (!_isStarted)
            {
                return;     // don't do anything before OnNetworkSpawn has run.
            }
            
            if (_destroyAtSec < Time.fixedTime)
            {
                OnEnd();
                Kill();
            }
        }

        public void OnEnd()
        {
            DetectCollisions(transform.position, Description.Radius, true);
        }
    }
}