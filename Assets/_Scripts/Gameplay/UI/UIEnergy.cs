using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Gameplay.UI
{
    public class UIEnergy: MonoBehaviour
    {
        [SerializeField]
        Slider m_EnergySlider;

        NetworkVariable<int> _networkEnergy;
        
        public void Initialize(NetworkVariable<int> networkedEnergy, int maxValue)
        {
            _networkEnergy = networkedEnergy;

            m_EnergySlider.minValue = _networkEnergy.Value;
            m_EnergySlider.maxValue = maxValue;
            EnergyChanged(0, networkedEnergy.Value);

            _networkEnergy.OnValueChanged += EnergyChanged;
        }
        
        void EnergyChanged(int previousValue, int newValue)
        {
            m_EnergySlider.value = newValue;
        }

        void OnDestroy()
        {
            _networkEnergy.OnValueChanged -= EnergyChanged;
        }
    }
}