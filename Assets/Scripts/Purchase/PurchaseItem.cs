using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class PurchaseItem : MonoBehaviour
{
    [Header("Required References")]
    public Button PurchaseButton;
    public GameObject OwnedPanel;

    public TMP_Text PriceText;

    public string ProductIdPrefix;
    public string ProductId;

    void Start()
    {
        if(string.IsNullOrWhiteSpace(ProductId) || !PriceText || !PurchaseButton)
        {
            Debug.LogError($"PurchaseItem \"{gameObject.name}\" incorrectly configured");
            return;
        }
        ProductIdPrefix ??= "";
        var product = PurchaseManager.Instance.GetProduct($"{ProductIdPrefix}{ProductId}");
        if(product?.definition.type != ProductType.Consumable)
        {
            SetOwned(product?.hasReceipt ?? false);
        }
        PriceText.text = product.metadata.localizedPriceString;
        PurchaseManager.Instance.PurchaseProcessed += OnPurchaseProcessed;
    }

    private void OnDestroy()
    {
        PurchaseManager.Instance.PurchaseProcessed -= OnPurchaseProcessed;
    }

    public void InitiatePurchase()
    {
        if (string.IsNullOrWhiteSpace($"{ProductIdPrefix}{ProductId}"))
        {
            Debug.LogError($"Cannot initiate purchase from IAPButtonView {gameObject.name}, no ProductId specified");
        }
        PurchaseManager.Instance.InitiatePurchase($"{ProductIdPrefix}{ProductId}");
    }

    private void OnPurchaseProcessed(PurchaseEventArgs purchaseEvent)
    {
        if(purchaseEvent.purchasedProduct.definition.id == $"{ProductIdPrefix}{ProductId}" &&
            purchaseEvent.purchasedProduct.definition.type != ProductType.Consumable)
        {
            SetOwned(true);
        }
    }

    public void SetOwned(bool owned)
    {
        PurchaseButton.gameObject.SetActive(!owned);
        OwnedPanel.SetActive(owned);
    }
}
