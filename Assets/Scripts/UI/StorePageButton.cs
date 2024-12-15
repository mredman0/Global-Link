using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StorePageButton : MonoBehaviour
{
    public Button Button;

    void Start()
    {
        Button.interactable = PurchaseManager.Instance.IsInitialized;
        PurchaseManager.Instance.Initialized += OnPurchaseManagerInitialized;
    }

    private void OnDestroy()
    {
        PurchaseManager.Instance.Initialized -= OnPurchaseManagerInitialized;
    }

    private void OnPurchaseManagerInitialized()
    {
        Button.interactable = true;
    }
}
