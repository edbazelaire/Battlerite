using _Scripts.Gameplay.GameplayObjects;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.UI
{
    /// <summary>
    /// Class containing references to UI children that we can display. Both are disabled by default on prefab.
    /// </summary>
    public class UIStateDisplay : MonoBehaviour
    {
        [SerializeField]
        UIName m_UIName;

        [SerializeField]
        UIHealth m_UIHealth;
        
        [SerializeField]
        UIMana m_UIMana;
        
        [SerializeField]
        UIEffect m_UIEffect;

        public void DisplayName(NetworkVariable<FixedPlayerName> networkedName)
        {
            if (m_UIName != null)
            {
                m_UIName.gameObject.SetActive(true);
                m_UIName.Initialize(networkedName);
            }
        }

        public void DisplayHealth(NetworkVariable<int> networkedHealth, int maxValue)
        {
            m_UIHealth.gameObject.SetActive(true);
            m_UIHealth.Initialize(networkedHealth, maxValue);
        }

        public void DisplayMana(NetworkVariable<float> manaReserve, NetworkVariable<float> manaPool, float maxManaReserve, float maxManaPool)
        {
            m_UIMana.gameObject.SetActive(true);
            m_UIMana.Initialize(manaReserve, manaPool, maxManaReserve, maxManaPool);
        }
        
        public void DisplayEffect(string name, float maxValue, float currentValue, Color color)
        {
            m_UIEffect.gameObject.SetActive(true);
            m_UIEffect.Initialize(name, maxValue, currentValue, color);
        }

        public void HideHealth()
        {
            m_UIHealth.gameObject.SetActive(false);
        }

        public void HideName()
        {
            if (m_UIName == null) { return; }
            
            m_UIName.gameObject.SetActive(false);
        }

        public void HideEffect()
        {
            if (m_UIEffect == null) { return; }
            
            m_UIEffect.gameObject.SetActive(false);
        }
    }
}
