using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderHandler : MonoBehaviour
{
    [HideInInspector] public Slider slider;

    private void Start()
    {
        if (slider == null)
        {
            SetSlider();
        }
    }

    public void Init(int value, int maxValue)
    {
        slider.maxValue = maxValue;
        slider.value = value;
    }
    public void Init(float value, float maxValue)
    {
        slider.maxValue = maxValue;
        slider.value = value;
    }

    public void SetValue(int value)
    {
        slider.value = value;
    }
    
    public void SetValue(float value)
    {
        slider.value = value;
    }

    public void SetSlider()
    {
        slider = GetComponent<Slider>();

        if (slider == null)
        {
            throw new Exception("unable to find slider in : " + name);
        }
    }
}
