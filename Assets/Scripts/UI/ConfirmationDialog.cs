using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ConfirmationDialog : Dialog
{
    public TMP_Text messageText;

    private Action onConfirm;

    private static readonly Action NOP = () => { };

    public void Show(string message, Action confirm = null, Action cancel = null)
    {
        onConfirm = confirm ?? NOP;
        onCancel = cancel ?? NOP;
        messageText.text = message;
        dialogPanel.SetActive(true);
    }

    public void Confirm()
    {
        onConfirm?.Invoke();
        Hide();
    }
}
