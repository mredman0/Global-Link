using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseItem : MonoBehaviour
{
    public Button PurchaseButton;
    public GameObject OwnedPanel;

    private bool Owned;

    public void SetOwned(bool owned)
    {
        Owned = owned;
        PurchaseButton.gameObject.SetActive(!owned);
        OwnedPanel.SetActive(owned);
    }
}
