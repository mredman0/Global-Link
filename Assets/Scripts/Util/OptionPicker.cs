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

public class OptionPicker : MonoBehaviour
{
    [Header("Required References")]
    public TMP_Text ValueText;
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
        UpdateValueText();
    }

    public void ShowOptions()
    {
        OptionsDialog.Show();
    }

    public void SetValue(string value)
    {
        Value = value;
        UpdateValueText();
        OnValueChanged?.Invoke(Value);
    }

    public void UpdateValueText()
    {
        ValueText.GetComponent<LocalizeStringEvent>().RefreshString();
    }
}
