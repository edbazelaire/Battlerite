using System;
using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Gameplay.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace _Scripts.Gameplay.GameplayObjects.Objects
{
    public class ServerDestructibleObject: NetworkBehaviour
    {
        [SerializeField]
        DamageReceiver m_DamageReceiver;

        [SerializeField]
        NetworkHealthState m_NetworkHealthState;
        
        [SerializeField]
        int MAX_HP;

        public int HP {
            get => m_NetworkHealthState.HitPoints.Value;
            private set => m_NetworkHealthState.HitPoints.Value = value;
        }

        public event Action<ServerCharacter> DestroyedObject;
        
        public override void OnNetworkSpawn()
        {
            if (!IsServer) { 
                enabled = false;
                return;
            }

            m_DamageReceiver.DamageReceived += OnHit;
            DestroyedObject += OnDestroyed;
            
            InitializeHitPoints();

        }

        public override void OnNetworkDespawn()
        {
            m_DamageReceiver.DamageReceived -= OnHit;
        }

        private void InitializeHitPoints()
        {
            HP = MAX_HP;
        }

        /// <summary>
        /// When the DestructibleObject is hit
        /// </summary>
        /// <param name="inflicter">Player who has inflicted the damages</param>
        /// <param name="HP">Number of HP to receive (inf 0 for damages) --> change ReceiveHP() function into hit()
        /// and heal() functions </param>
        private void OnHit(ServerCharacter inflicter, int hp)
        {
            if (hp == 0) { return; }

            Assert.IsFalse(hp > 0,
                "can't add positive HP to a destructible object");

            HP += hp;

            if (HP <= 0)
            {
                DestroyedObject?.Invoke(inflicter);
            }
        }

        private void OnDestroyed(ServerCharacter inflicter)
        {
            inflicter.GetComponent<IDamageable>().ReceiveHP(null, 200);
        }
    }
}