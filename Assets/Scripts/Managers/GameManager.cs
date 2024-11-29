using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class GameManager : MonoBehaviour
{
    public const string TUTORIAL_SHOWN_KEY = "TutorialShown";

    public static GameManager Instance;

    private void Start()
    {
        if(Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Application.targetFrameRate = 120;
        DontDestroyOnLoad(gameObject);

        if(HasTutorialBeenShown())
        {
            var puzzleLoader = GetComponent<PuzzleLoader>();
            puzzleLoader.PuzzlePack = "Tutorial";
            puzzleLoader.PuzzleIdInPack = "1";
            puzzleLoader.LoadPuzzle();
        }
    }

    public bool HasTutorialBeenShown() => PlayerPrefs.GetInt(TUTORIAL_SHOWN_KEY, 0) == 0;
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
