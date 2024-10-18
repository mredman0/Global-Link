using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageNavigator : MonoBehaviour
{
    public MenuManager MenuManager;
    public MenuPage PageToNavigateTo;

    public void GotoPage()
    {
        MenuManager.GotoPage(PageToNavigateTo);
    }
}
