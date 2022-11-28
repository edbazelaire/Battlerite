using _Scripts.Gameplay.GameplayObjects.Character;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects.Abilities
{
    public class ServerShieldAbility: ServerAbility
    {
        // =================================================================================================================
        // GameObject properties
        private void OnTriggerEnter(Collider collision)
        {
            // -------------------------------------------------------------------------------------------------------------
            // Counter a Projectile
            if (collision.GetComponent<ServerProjectileLogic>() != null)
            {
                var projectile = collision.GetComponent<ServerProjectileLogic>();
                var enemyId = projectile.SpawnerId;
            
                // dont block ally projectiles
                if (GameHandler.Instance.IsAlly(_spawnerId, enemyId))
                {
                    return;
                }

                TriggerProjectileEffect(projectile);
                
                projectile.Kill();
            }
            
            // -------------------------------------------------------------------------------------------------------------
            // Counter a Melee Ability
            else if (collision.GetComponent<ServerMeleeAbility>() != null)
            {
                var ability = collision.GetComponent<ServerMeleeAbility>();
                TriggerMeleeEffect(ability);
            }
        }
        
        protected virtual void TriggerProjectileEffect(ServerProjectileLogic ability) { }
        protected virtual void TriggerMeleeEffect(ServerMeleeAbility ability) { }
    }
}