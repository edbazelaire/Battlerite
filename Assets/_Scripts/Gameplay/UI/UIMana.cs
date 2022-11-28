using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Gameplay.UI
{
    public class UIMana: MonoBehaviour
    {
        [SerializeField]
        Slider m_ManaReserveSlider;
        [SerializeField]
        Slider m_ManaPoolSlider;

        private NetworkVariable<float> _manaReserve;
        private NetworkVariable<float> _manaPool;
        
        public void Initialize(NetworkVariable<float> manaReserve, NetworkVariable<float> manaPool, float maxManaReserve, float maxManaPool)
        {
            _manaReserve = manaReserve;
            _manaPool = manaPool;

            m_ManaReserveSlider.minValue = 0;
            m_ManaReserveSlider.maxValue = maxManaReserve;
            ManaReserveChanged(0, manaReserve.Value);

            m_ManaPoolSlider.minValue = 0;
            m_ManaPoolSlider.maxValue = maxManaPool;
            ManaPoolChanged(0, manaPool.Value);
            
            _manaReserve.OnValueChanged += ManaReserveChanged;
            _manaPool.OnValueChanged += ManaPoolChanged;
        }
        
        void ManaReserveChanged(float previousValue, float newValue)
        {
            m_ManaReserveSlider.value = newValue;
        }
        
        void ManaPoolChanged(float previousValue, float newValue)
        {
            m_ManaPoolSlider.value = newValue;
        }

        void OnDestroy()
        {
            _manaReserve.OnValueChanged -= ManaReserveChanged;
            _manaPool.OnValueChanged -= ManaPoolChanged;
        }
    }
}