using _Scripts.Gameplay.GameplayObjects.Abilities;
using _Scripts.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.Actions
{
    /// <summary>
    /// Action responsible for creating a projectile object.
    /// </summary>
    public class LaunchProjectileAction : Action
    {
        public LaunchProjectileAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        /// <summary>
        /// Instantiates and configures the arrow. Repeatedly calling this does nothing.
        /// </summary>
        /// <remarks>
        /// This calls GetProjectilePrefab() to find the prefab it should instantiate.
        /// </remarks>
        public override void Activate()
        {
            // NetworkObject no = NetworkObjectPool.Singleton.GetNetworkObject(projectileInfo.ProjectilePrefab, projectileInfo.ProjectilePrefab.transform.position, projectileInfo.ProjectilePrefab.transform.rotation);
            var no = GameObject.Instantiate(Description.Prefab);
            
            // point the projectile the same way we're facing
            no.transform.forward = m_Parent.physicsWrapper.Transform.forward;

            //this way, you just need to "place" the arrow by moving it in the prefab, and that will control
            //where it appears next to the player.
            no.transform.position = m_Parent.physicsWrapper.Transform.localToWorldMatrix.MultiplyPoint(no.transform.position);

            no.GetComponent<ServerProjectileLogic>().Initialize(m_Parent.NetworkObjectId, Description, GetFinalRange(m_Parent.NetState.MousePosition.Value));

            no.GetComponent<NetworkObject>().Spawn(true);
        }
        
        public override void Cancel()
        {
            if (!string.IsNullOrEmpty(Description.CancelAnimation))
            {
                m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.CancelAnimation);
            }
        }
    }
}
