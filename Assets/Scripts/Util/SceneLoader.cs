using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Settings")]
    public string DefaultSceneToLoad;
    public bool Additive = false;
    public bool AllowInterstitial;

    private string SceneToLoad;

    public void LoadScene(string name = "")
    {
        SceneToLoad = string.IsNullOrWhiteSpace(name) ? DefaultSceneToLoad : name;
        if (AllowInterstitial)
        {
            AdManager.Instance.InterstitialClosed += DoLoad;
            if (!AdManager.Instance.ShowInterstitial())
            {
                AdManager.Instance.InterstitialClosed -= DoLoad;
                DoLoad();
            }
            return;
        }
        DoLoad();
    }

    private void DoLoad()
    {
        if(AllowInterstitial)
        {
            AdManager.Instance.InterstitialClosed -= DoLoad;
        }
        Addressables.LoadSceneAsync(SceneToLoad, Additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
    }
}
