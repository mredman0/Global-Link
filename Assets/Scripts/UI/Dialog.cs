using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dialog : MonoBehaviour
{
    public GameObject dialogPanel;

    protected Action onCancel;

    public void Cancel()
    {
        Hide();
        onCancel?.Invoke();
    }

    public void Show()
    {
        dialogPanel.SetActive(true);
        OnShown();
    }

    public void Hide()
    {
        dialogPanel.SetActive(false);
        OnHidden();
    }

    protected virtual void OnShown() { }
    protected virtual void OnHidden() { }
}
