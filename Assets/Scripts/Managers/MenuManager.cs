using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static Dictionary<string, string> SavedPages = new Dictionary<string, string>();

    public string MenuID;
    private readonly List<MenuPage> Pages = new List<MenuPage>();
    private MenuPage CurrentPage;

    // Start is called before the first frame update
    void Start()
    {
        Pages.AddRange(GetComponentsInChildren<MenuPage>(includeInactive: true));
        CurrentPage = Pages.FirstOrDefault(p => p.gameObject.activeSelf);
        if(SavedPages.TryGetValue(MenuID, out string savedPage))
        {
            if (!string.IsNullOrWhiteSpace(savedPage))
            {
                var pageToReturnTo = Pages.FirstOrDefault(p => p.name == savedPage);
                if (pageToReturnTo)
                {
                    GotoPage(pageToReturnTo);
                }
            }
            SavedPages.Remove(MenuID);
        }

        InputManager.Instance.AddBackAction(this, GoBack);
    }

    private void OnDestroy()
    {
        SavedPages.Add(MenuID, CurrentPage ? CurrentPage.name : null);
        InputManager.Instance.RemoveBackAction(this);
    }

    public void GotoPage(MenuPage page)
    {
        foreach(var p in Pages)
        {
            p.SetVisible(p == page);
        }
        CurrentPage = page;
    }

    public void GoBack() => CurrentPage.GoBack();
}
