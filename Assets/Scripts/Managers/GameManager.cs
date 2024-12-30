using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;

public class GameManager : MonoBehaviour
{
    public const string TUTORIAL_SHOWN_KEY = "TutorialShown";

    public static GameManager Instance;

    [Header("Required References")]
    public RectTransform TutorialDialogParent;
    public GameObject ShowTutorialDialogPrefab;

    [Header("Settings")]
    public int TargetFrameRate = 120;

    private SynchronizationContext MainThreadContext;
    private ConfirmationDialog ShowTutorialDialog;

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

        Application.targetFrameRate = TargetFrameRate;

        if (UnityServicesManager.Instance.Initialized)
        {
            ShowTutorialDialogIfNeeded();
        }
        UnityServicesManager.Instance.ServicesInitialized += ShowTutorialDialogIfNeeded;
        UnityServicesManager.Instance.ServicesInitializationFailed += ShowTutorialDialogIfNeeded;
    }

    private void OnDestroy()
    {
        UnityServicesManager.Instance.ServicesInitialized -= ShowTutorialDialogIfNeeded;
        UnityServicesManager.Instance.ServicesInitializationFailed -= ShowTutorialDialogIfNeeded;
    }

    private void ShowTutorialDialogIfNeeded()
    {
        if (SynchronizationContext.Current != MainThreadContext)
        {
            MainThreadContext.Post(_ => ShowTutorialDialogIfNeeded(), null);
            return;
        }

        if (!HasTutorialBeenShown())
        {
            var dialogGO = Instantiate(ShowTutorialDialogPrefab, TutorialDialogParent);
            ShowTutorialDialog = dialogGO.GetComponent<ConfirmationDialog>();
            ShowTutorialDialog.Show(OnTutorialDialogConfirm, OnTutorialDialogCancel);
        }
    }

    public bool HasTutorialBeenShown() => PlayerPrefs.GetInt(TUTORIAL_SHOWN_KEY, 0) > 0;
    public void SetTutorialShown() => PlayerPrefs.SetInt(TUTORIAL_SHOWN_KEY, 1);
    public void ResetTutorialShown() => PlayerPrefs.SetInt(TUTORIAL_SHOWN_KEY, 0);

    private void OnTutorialDialogConfirm()
    {
        SetTutorialShown();
        var puzzleLoader = GetComponent<PuzzleLoader>();
        puzzleLoader.PuzzlePack = "Tutorial";
        puzzleLoader.PuzzleIdInPack = "1";
        puzzleLoader.LoadPuzzle();
    }

    private void OnTutorialDialogCancel()
    {
        Destroy(ShowTutorialDialog.gameObject);
        SetTutorialShown();
    }

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
