using System;
using _Scripts.Gameplay.GameplayObjects;
using _Scripts.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

namespace _Scripts.Gameplay.Actions
{
    public class CounterAction: Action
    {
        private ServerAbility _instance;

        public CounterAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
            
        }
        
        public override void Activate()
        {
            Assert.IsNotNull(Description.Prefab, $"{Description.name} has no prefab for counter");
            
            var no = GameObject.Instantiate(Description.Prefab, m_Parent.transform);
            no.GetComponent<NetworkObject>().Spawn(true);
            _instance = no.GetComponent<ServerAbility>();

            // set collider of the counter just around the collider of the character
            AddParentColliderToAbility();
        }

        private void AddParentColliderToAbility()
        {
            var parentCollider = m_Parent.physicsWrapper.DamageCollider;
            _instance.AddComponent<Collider>();
            
            Type type = parentCollider.GetType();
            Component copy = _instance.AddComponent<Collider>();
            
            // Copied fields can be restricted with BindingFlags
            System.Reflection.FieldInfo[] fields = type.GetFields(); 
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(parentCollider));
            }
            
            // increase size of the collider by a little bit to catch projectiles before parent collider
            _instance.transform.localScale *= 1.05f;
        }
        
        public override void Cancel()
        {
            _instance.Kill();
        }
    }
}