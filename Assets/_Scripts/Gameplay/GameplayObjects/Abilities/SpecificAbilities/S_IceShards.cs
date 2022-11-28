using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Utils.Enums;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects.Abilities.SpecificAbilities
{
    public class S_IceShards: ServerShieldAbility
    {
        public float ExplosionRadius = 3;
        public float KnockBackSpeed = 3;

        private Collider[] m_CollisionCache = new Collider[k_MaxCollisions];
        private const int k_MaxCollisions = 10;
    
        protected override void TriggerProjectileEffect(ServerProjectileLogic ability)
        {
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_spawnerId, out var spawnerNet);
            var spawner = spawnerNet != null ? spawnerNet.GetComponent<ServerCharacter>() : null;
            
            spawner.ReceiveShield(25);
        }

        protected override void TriggerMeleeEffect(ServerMeleeAbility ability)
        {
            Explode();
        }

        private void Explode()
        {
            var numCollisions = Physics.OverlapSphereNonAlloc(transform.position, ExplosionRadius, m_CollisionCache, LayerMask.GetMask("Player"));

            for (int i = 0; i < numCollisions; i++)
            {
                // check if object is a NetworkObject and not the spell caster
                var targetNetObj = m_CollisionCache[i].GetComponentInParent<NetworkObject>();
                if (! targetNetObj || targetNetObj.NetworkObjectId == _spawnerId)
                {
                    continue;
                }
                    
                // all hittable layer entities should have one of these.
                targetNetObj.GetComponent<ServerCharacterMovement>().StartKnockback(transform.position, KnockBackSpeed, ExplosionRadius);
                targetNetObj.GetComponent<ServerCharacterEffects>().AddAbilityEffect(AbilityEffectEnum.FROST, 0);
            }
        } 
    }
}