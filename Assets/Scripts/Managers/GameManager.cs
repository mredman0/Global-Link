using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class GameManager : MonoBehaviour
{
    public const string TUTORIAL_SHOWN_KEY = "TutorialShown";

    public static GameManager Instance;

    private SynchronizationContext MainThreadContext;

    private void Start()
    {
        if(Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        MainThreadContext = SynchronizationContext.Current;

        Application.targetFrameRate = 120;

        Debug.Log($"GameManager startup");
        if (UnityServicesManager.Instance.Initialized)
        {
            Debug.Log($"GameManager: UnityServices already initialized");
            ShowTutorialIfNeeded();
        }
        UnityServicesManager.Instance.ServicesInitialized += ShowTutorialIfNeeded;
        UnityServicesManager.Instance.ServicesInitializationFailed += ShowTutorialIfNeeded;
        Debug.Log($"GameManager hooked UnityServicesManager events");
    }

    private void OnDestroy()
    {
        UnityServicesManager.Instance.ServicesInitialized -= ShowTutorialIfNeeded;
        UnityServicesManager.Instance.ServicesInitializationFailed -= ShowTutorialIfNeeded;
        Debug.Log($"GameManager UNHOOKED UnityServicesManager events");
    }

    private void ShowTutorialIfNeeded()
    {
        if (SynchronizationContext.Current != MainThreadContext)
        {
            MainThreadContext.Post(_ => ShowTutorialIfNeeded(), null);
            return;
        }

        Debug.Log($"GameManager: Showing tutorial if needed. Has been shown already? {HasTutorialBeenShown()}");
        if (!HasTutorialBeenShown())
        {
            var puzzleLoader = GetComponent<PuzzleLoader>();
            puzzleLoader.PuzzlePack = "Tutorial";
            puzzleLoader.PuzzleIdInPack = "1";
            puzzleLoader.LoadPuzzle();
        }
    }

    public bool HasTutorialBeenShown() => PlayerPrefs.GetInt(TUTORIAL_SHOWN_KEY, 0) > 0;
    public void SetTutorialShown() => PlayerPrefs.SetInt(TUTORIAL_SHOWN_KEY, 1);
    public void ResetTutorialShown() => PlayerPrefs.SetInt(TUTORIAL_SHOWN_KEY, 0);

    public static bool AssetExists<T>(string key)
    {
        foreach (var locator in Addressables.ResourceLocators)
        {
            if (locator.Locate(key, typeof(T), out _))
            {
                return true;
            }
        }
        return false;
    }
}
