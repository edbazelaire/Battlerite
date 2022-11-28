using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Gameplay.UI
{
    public class UIEffect: MonoBehaviour
    {
        [SerializeField]
        Slider m_EffectSlider;

        [SerializeField] 
        TMP_Text m_AbilityEffectNameText;

        [SerializeField] 
        Image m_SliderFillImage;

        private float _currentValue;

        public void Initialize(string abilityName, float maxValue, float currentValue, Color color)
        {
            _currentValue = currentValue;
            
            m_EffectSlider.minValue = 0;
            m_EffectSlider.maxValue = maxValue;
            m_EffectSlider.value = currentValue;

            m_AbilityEffectNameText.text = abilityName;
            m_AbilityEffectNameText.color = color;
            m_SliderFillImage.color = color;
        }

        public void Update()
        {
            _currentValue -= Time.deltaTime;
            if (_currentValue < 0)
            {
                _currentValue = 0;
            }
            m_EffectSlider.value = _currentValue;
        }
    }
}