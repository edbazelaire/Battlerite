using _Scripts.Gameplay.GameplayObjects.Abilities;
using _Scripts.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.Actions
{
    /// <summary>
    /// Action responsible for creating a physics-based thrown object.
    /// </summary>
    public class TossAction : Action
    {
        public TossAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override bool Start()
        {
            m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
            return true;
        }

        public override bool Update()
        {
            return true;
        }
        
        /// <summary>
        /// Instantiates and configures the thrown object. Repeatedly calling this does nothing.
        /// </summary>
        /// <remarks>
        /// This calls GetProjectilePrefab() to find the prefab it should instantiate.
        /// </remarks>
        public override void Activate()
        {
            // var no = NetworkObjectPool.Singleton.GetNetworkObject(projectileInfo.ProjectilePrefab, projectileInfo.ProjectilePrefab.transform.position, projectileInfo.ProjectilePrefab.transform.rotation);
            var no = GameObject.Instantiate(Description.Prefab).GetComponent<NetworkObject>();
            var networkObjectTransform = no.transform;
            
            // set final position of the aoe
            var mousePosition = m_Parent.NetState.MousePosition.Value;
            Data.Position = GetFinalPosition(mousePosition);
            Data.Position.y = 0.01f;

            // point the thrown object the same way we're facing
            networkObjectTransform.forward = m_Parent.physicsWrapper.Transform.forward;
            networkObjectTransform.position = m_Parent.physicsWrapper.Transform.localToWorldMatrix.MultiplyPoint(networkObjectTransform.position);

            var tossObject = no.GetComponent<ServerTossedLogic>();
            tossObject.Initialize(m_Parent.NetworkObjectId, Description, Data.Position);

            no.Spawn(true);
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
