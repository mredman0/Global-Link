using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Incrementor : MonoBehaviour
{
    [Header("Required References")]
    public TMP_Text Display;
    public Button IncrementButton;
    public Button DecrementButton;

    [Header("Settings")]
    public float Min = 0;
    public float Max = 100;
    public float Step = 10;
    public float DisplayFactor = 1f;
    public string DisplayFormat = "{0}";


    [Serializable]
    public class OnChangeEvent : UnityEvent<float> { }
    public OnChangeEvent OnValueChanged;

    [Header("State")]
    public float Value;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Increment()
    {
        var prev = Value;
        Value = Mathf.Min(Value + Step, Max);
        UpdateDisplay();
        if (Value != prev)
        {
            OnValueChanged?.Invoke(Value);
        }
    }

    public void Decrement()
    {
        var prev = Value;
        Value = Mathf.Max(Value - Step, Min);
        UpdateDisplay();
        if (Value != prev)
        {
            OnValueChanged?.Invoke(Value);
        }
    }

    public void SetValue(float val) => SetValueInternal(val, raiseChangedEvent: true);
    public void SetValueWithoutNotify(float val) => SetValueInternal(val, raiseChangedEvent: false);

    private void SetValueInternal(float val, bool raiseChangedEvent)
    {
        float remainder = (val - Min) % Step;
        val = Mathf.Clamp(val - remainder, Min, Max);
        var prev = Value;
        Value = val;
        UpdateDisplay();
        if (raiseChangedEvent && Value != prev)
        {
            OnValueChanged?.Invoke(Value);
        }
    }

    private void UpdateDisplay()
    {
        Display.text = string.Format(DisplayFormat, Value * DisplayFactor);
        IncrementButton.interactable = Value < Max;
        DecrementButton.interactable = Value > Min;
    }
}
