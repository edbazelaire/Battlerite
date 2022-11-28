using Unity.Netcode;

namespace _Scripts.Gameplay.GameplayObjects
{
    /// <summary>
    /// Shared state for a Projectile.
    /// </summary>
    public class NetworkAbilityState : NetworkBehaviour
    {
        /// <summary>
        /// This event is raised when the projectile hit an enemy. The argument is the NetworkObjectId of the enemy.
        /// </summary>
        public System.Action<ulong> HitEnemyEvent;

        [ClientRpc]
        public void RecvHitEnemyClientRPC(ulong enemyId)
        {
            HitEnemyEvent?.Invoke(enemyId);
        }
    }
}
