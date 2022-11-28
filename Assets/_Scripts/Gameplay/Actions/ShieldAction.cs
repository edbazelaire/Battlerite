using _Scripts.Gameplay.GameplayObjects;
using _Scripts.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace _Scripts.Gameplay.Actions
{
    public class ShieldAction: Action
    {
        private ServerAbility _instance;

        public ShieldAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override void Activate()
        {
            Assert.IsNotNull(Description.Prefab , 
                $"{Description.name} has no prefab for shield");

            var no = GameObject.Instantiate(Description.Prefab, m_Parent.transform);
            
            // point the projectile the same way we're facing
            no.transform.forward = m_Parent.physicsWrapper.Transform.forward;
            no.transform.forward = m_Parent.transform.forward;
            no.transform.localPosition = new Vector3(0, 1, 0.5f);
            
            no.GetComponent<NetworkObject>().Spawn(true);

            _instance = no.GetComponent<ServerAbility>();
        }

        public override void Cancel()
        {
            _instance.Kill();
        }
    }
}