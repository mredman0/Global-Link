using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class ConfirmationDialog : Dialog
{
    public TMP_Text messageText;
    public LocalizeStringEvent messageLoc;

    private Action onConfirm;

    private static readonly Action NOP = () => { };

    public void Show(LocalizedString message, Action confirm = null, Action cancel = null)
    {
        onConfirm = confirm ?? NOP;
        onCancel = cancel ?? NOP;
        messageLoc.StringReference = message;
        Show();
    }

    public void Show(string message, Action confirm = null, Action cancel = null)
    {
        onConfirm = confirm ?? NOP;
        onCancel = cancel ?? NOP;
        messageText.text = message;
        Show();
    }

    public void Confirm()
    {
        onConfirm?.Invoke();
        Hide();
    }
}
