using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.Configuration.AbilityEffects;
using _Scripts.Gameplay.GameplayObjects.Character;
using Unity.Multiplayer.Samples.BossRoom;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers
{
    public class GameDataSource : MonoBehaviour
    {
        [Tooltip("All CharacterClass data should be slotted in here")]
        [SerializeField]
        private CharacterClass[] m_CharacterData;

        [Tooltip("All ActionDescription data should be slotted in here")]
        [SerializeField]
        private ActionDescription[] m_ActionData;

        [Tooltip("List of AbilityEffects")]
        [SerializeField]
        private AbilityEffect[] m_AbilityEffectData;

        private Dictionary<string, CharacterClass> m_CharacterDataMap;
        private Dictionary<string, ActionDescription> m_ActionDataMap;
        private Dictionary<string, AbilityEffect> m_AbilityEffectMap;

        /// <summary>
        /// static accessor for all GameData.
        /// </summary>
        public static GameDataSource Instance { get; private set; }

        /// <summary>
        /// Contents of the CharacterData list, indexed by CharacterType for convenience.
        /// </summary>
        public Dictionary<string, CharacterClass> CharacterDataByType
        {
            get
            {
                if (m_CharacterDataMap == null)
                {
                    m_CharacterDataMap = new Dictionary<string, CharacterClass>();
                    foreach (CharacterClass data in m_CharacterData)
                    {
                        m_CharacterDataMap[data.CharacterName] = data;
                    }
                }
                return m_CharacterDataMap;
            }
        }

        /// <summary>
        /// Contents of the ActionData list, indexed by ActionType for convenience.
        /// </summary>
        public Dictionary<string, ActionDescription> ActionDataByName
        {
            get
            {
                if (m_ActionDataMap == null)
                {
                    // Add data specified in given m_ActionData
                    m_ActionDataMap = new Dictionary<string, ActionDescription>();
                    foreach (ActionDescription data in m_ActionData)
                    {
                        m_ActionDataMap[data.ActionName] = data;
                    }
                    
                    // add Characters Abilities as data
                    foreach (CharacterClass charClass in m_CharacterData)
                    {
                        foreach (var data in charClass.Abilities)
                        {
                            if (! m_ActionDataMap.ContainsKey(data.name))
                            {
                                m_ActionDataMap[data.ActionName] = data;
                            }
                        }
                    }
                }
                
                return m_ActionDataMap;
            }
        }

        /// <summary>
        /// Contents of the ActionData list, indexed by ActionType for convenience.
        /// </summary>
        public Dictionary<string, AbilityEffect> AbilityEffectsByName
        {
            get
            {
                if (m_AbilityEffectMap == null)
                {
                    m_AbilityEffectMap = new Dictionary<string, AbilityEffect>();
                    foreach (AbilityEffect data in m_AbilityEffectData)
                    {
                        var effectName = data.name;
                        if (m_AbilityEffectMap.ContainsKey(effectName))
                        {
                            throw new System.Exception($"Duplicate action definition detected: {effectName}");
                        }
                        m_AbilityEffectMap[effectName] = data;
                    }
                }
                
                
                return m_AbilityEffectMap;
            }
        }

        private void Awake()
        {
            if (Instance != null)
            {
                throw new Exception("Multiple GameDataSources defined!");
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
    }
}
