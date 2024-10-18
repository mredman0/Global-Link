using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static string SavedPage;

    public string MenuID;
    private readonly List<MenuPage> Pages = new List<MenuPage>();
    private MenuPage CurrentPage;

    // Start is called before the first frame update
    void Start()
    {
        Pages.AddRange(GetComponentsInChildren<MenuPage>(includeInactive: true));
        CurrentPage = Pages.FirstOrDefault(p => p.gameObject.activeSelf);
        if(!string.IsNullOrWhiteSpace(SavedPage))
        {
            var pageToReturnTo = Pages.FirstOrDefault(p => p.name == SavedPage);
            if(pageToReturnTo)
            {
                GotoPage(pageToReturnTo);
            }
        }
        SavedPage = null;
    }

    private void OnDestroy()
    {
        SavedPage = CurrentPage ? CurrentPage.name : null;
    }

    public void GotoPage(MenuPage page)
    {
        foreach(var p in Pages)
        {
            p.SetVisible(p == page);
        }
        CurrentPage = page;
    }
}
