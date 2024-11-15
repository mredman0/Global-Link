using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionsDialog : Dialog
{
    public event Action<string> OptionSelected;

    [Header("Settings")]
    public bool HideOnOptionSelected = false;

    public void SelectOption(string option)
    {
        OptionSelected?.Invoke(option);
        if(HideOnOptionSelected)
        {
            Hide();
        }
    }
}
