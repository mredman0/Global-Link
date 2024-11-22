using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class SpecialOptionPicker : MonoBehaviour
{
    [Header("Required References")]
    public List<GameObject> OptionDisplays;
    public OptionsDialog OptionsDialog;

    [Serializable]
    public class OnChangeEvent : UnityEvent<string> { }
    [Header("Settings")]
    public OnChangeEvent OnValueChanged;

    [Header("State")]
    public string Value;

    // Start is called before the first frame update
    void Start()
    {
        OptionsDialog.OptionSelected += SetValue;
        UpdateDisplay();
    }

    public void ShowOptions()
    {
        OptionsDialog.Show();
    }

    public void SetValue(string value)
    {
        Value = value;
        UpdateDisplay();
        OnValueChanged?.Invoke(Value);
    }

    public void UpdateDisplay()
    {
        foreach(var obj in OptionDisplays)
        {
            obj.SetActive(obj.name == Value);
        }
    }
}
