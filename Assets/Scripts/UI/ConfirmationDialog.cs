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
    [Header("Required References")]
    public TMP_Text messageText;
    public LocalizeStringEvent messageLoc;

    [Header("Optional References")]
    public Button BackButton;
    public Button ConfirmButton;
    public Button CancelButton;

    [Header("Settings")]
    public float ButtonsDisabledSeconds = 0;

    private Action onConfirm;

    private static readonly Action NOP = () => { };

    public void Show(LocalizedString message, Action confirm = null, Action cancel = null)
    {
        messageLoc.StringReference = message;
        Show(confirm, cancel);
    }

    public void Show(string message, Action confirm = null, Action cancel = null)
    {
        messageText.text = message;
        Show(confirm, cancel);
    }

    public void Show(Action confirm = null, Action cancel = null)
    {
        if(ButtonsDisabledSeconds > 0)
        {
            SetButtonsActive(false);
            StartCoroutine(SetButtonsActiveDelayed(true, ButtonsDisabledSeconds));
        }
        else
        {
            SetButtonsActive(true);
        }

        onConfirm = confirm ?? NOP;
        onCancel = cancel ?? NOP;
        base.Show();
    }

    public void Confirm()
    {
        onConfirm?.Invoke();
        Hide();
    }

    private IEnumerator SetButtonsActiveDelayed(bool active, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetButtonsActive(active);
    }
    private void SetButtonsActive(bool active)
    {
        if(BackButton)
        {
            BackButton.interactable = active;
        }
        if (ConfirmButton)
        {
            ConfirmButton.interactable = active;
        }
        if (CancelButton)
        {
            CancelButton.interactable = active;
        }
    }
}
