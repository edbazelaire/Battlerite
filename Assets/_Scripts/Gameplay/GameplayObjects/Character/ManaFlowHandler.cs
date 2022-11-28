using System;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects.Character
{
    public class ManaFlowHandler
    {
        private ServerCharacter m_ServerCharacter;
        private int _manaFlowLevel = 3;
        public int ManaFlowLevel => _manaFlowLevel;
        private float _flowFactor = 1;

        // regen of the mana reserve per second
        private float _manaRegen = 5;
        
        public ManaFlowHandler(ServerCharacter serverCharacter)
        {
            m_ServerCharacter = serverCharacter;
            SetManaFlowLevel(3);
        }

        public void Update()
        {
            // update mana reserve with regen mana
            var deltaManaRegen = _manaRegen * Time.deltaTime;
            m_ServerCharacter.ReceiveManaReserve(deltaManaRegen);

            // flow mana from reserve to mana pool
            var manaFlow = deltaManaRegen * _flowFactor;
            manaFlow = Mathf.Min(manaFlow, m_ServerCharacter.NetState.ManaReserve);
            m_ServerCharacter.ReceiveMana(manaFlow);
            
            m_ServerCharacter.ReceiveManaReserve(-manaFlow);
        }

        public void SetManaFlowLevel(int level)
        {
            switch (level)
            {
                case(1):
                    _flowFactor = 0.2f;
                    break;
                
                case(2):
                    _flowFactor = 0.5f;
                    break;
                
                case(3):
                    _flowFactor = 1;
                    break;
                
                case(4):
                    _flowFactor = 2;
                    break;
                
                case(5):
                    _flowFactor = 4;
                    break;
                
                default:
                    throw new NotImplementedException("Unknown mana flow level " + level);
            }
            
            _manaFlowLevel = level;
        }
    }
}