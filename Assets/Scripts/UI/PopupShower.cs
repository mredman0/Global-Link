using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupShower : MonoBehaviour
{
    [Header("Required References")]
    public GameObject PopupPrefab;

    [Header("Optional References")]
    public Transform PopupRoot;

    private Dialog Popup;

    public void ShowPopup()
    {
        if(Popup)
        {
            HidePopup();
        }

        var root = PopupRoot;
        if(!root)
        {
            root = GetComponentInParent<Canvas>().transform;
        }

        var popupGO = Instantiate(PopupPrefab, root);
        Popup = popupGO.GetComponent<Dialog>();
    }

    public void HidePopup()
    {
        if(!Popup)
        {
            return;
        }
        Popup.Hide();
        Destroy(Popup.gameObject);
    }
}
