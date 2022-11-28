using _Scripts.Gameplay.GameplayObjects.Abilities;
using _Scripts.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.Actions
{
    /// <summary>
    /// Area-of-effect attack Action. The attack is centered on a point provided by the client.
    /// </summary>
    public class AoeAction : Action
    {
        public AoeAction(ServerCharacter parent, ref ActionRequestData data)
            : base(parent, ref data) { }

        public override bool Start()
        {
            m_Parent.serverAnimationHandler.NetworkAnimator.SetTrigger(Description.Anim);
            return true;
        }

        public override void Activate()
        {
            Data.Position = GetFinalPosition(m_Parent.NetState.MousePosition.Value);
            var clone = GameObject.Instantiate(Description.Prefab).GetComponent<ServerAoeLogic>();
            clone.Initialize(m_Parent.NetworkObjectId, Description, Data.Position);
            clone.GetComponent<NetworkObject>().Spawn(true);
        }

        public override bool Update()
        {
            return true;
        }
    }
}
