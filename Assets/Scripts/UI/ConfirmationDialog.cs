using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ConfirmationDialog : MonoBehaviour
{
    public GameObject dialogPanel; // Assign the panel in the Inspector
    public TMP_Text messageText; // Assign the Text component for the message

    private Action onConfirm;
    private Action onCancel;

    private static readonly Action NOP = () => { };

    private void Start()
    {

    }

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
        dialogPanel.SetActive(false);
    }

    public void Cancel()
    {
        onCancel?.Invoke();
        dialogPanel.SetActive(false);
    }
}
