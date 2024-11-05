using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public const string PLAYED_TUTORIAL_KEY = "played_tutorial";

    public static GameManager Instance;

    private void Start()
    {
        if(Instance)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        Application.targetFrameRate = 120;
        DontDestroyOnLoad(gameObject);

        if(PuzzleCompletionManager.Instance && !PuzzleCompletionManager.Instance.IsTutorialComplete())
        {
            var puzzleLoader = GetComponent<PuzzleLoader>();
            puzzleLoader.PuzzlePack = "Tutorial";
            puzzleLoader.PuzzleIdInPack = "1";
            puzzleLoader.LoadPuzzle();
        }
    }
}
