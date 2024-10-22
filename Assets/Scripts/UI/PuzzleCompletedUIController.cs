using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleCompletedUIController : MonoBehaviour
{
    [Header("Required References")]
    public Puzzle Puzzle;
    public CameraController CameraController;
    public GameObject PuzzleCompletedPanel;
    public PuzzleLoader NextLevelLoader;
    public GameObject UIWithNextLevel;
    public GameObject UIWithNoNextLevel;


    // Start is called before the first frame update
    void Start()
    {
        Puzzle.PuzzleCompleted += OnPuzzleCompleted;
        PuzzleCompletedPanel.SetActive(false);

        var nextLevel = GetNextLevelIfExists();
        if(nextLevel is null)
        {
            UIWithNextLevel.SetActive(false);
            UIWithNoNextLevel.SetActive(true);
        }
        else
        {
            UIWithNoNextLevel.SetActive(false);
            UIWithNextLevel.SetActive(true);
            NextLevelLoader.PuzzlePack = nextLevel.Value.pack;
            NextLevelLoader.PuzzleIdInPack = nextLevel.Value.idInPack;
        }
    }

    private (string pack, string idInPack)? GetNextLevelIfExists()
    {
        var currentPuzzleId = PuzzleProvider.Instance.PuzzleConfig.ID;
        var puzzleIdSplit = currentPuzzleId.Split('_');
        if(puzzleIdSplit.Length != 2)
        {
            return null;
        }
        var puzzlePack = puzzleIdSplit[0];
        var puzzleIdInPack = puzzleIdSplit[1];
        var idIsInt = int.TryParse(puzzleIdInPack, out int idInPackInt);
        if(!idIsInt)
        {
            return null;
        }
        var nextLevelId = (idInPackInt + 1).ToString();
        string resourcePath = $"Puzzles/{puzzlePack}/{puzzlePack}_{nextLevelId}";
        var puzzleConfig = Resources.Load<PuzzleConfig>(resourcePath);
        if (!puzzleConfig)
        {
            return null;
        }
        return (puzzlePack, nextLevelId);
    }

    private void OnDestroy()
    {
        Puzzle.PuzzleCompleted -= OnPuzzleCompleted;
    }

    private void OnPuzzleCompleted()
    {
        SetPuzzleCompletedUIVisible(true);
    }

    public void SetPuzzleCompletedUIVisible(bool visible)
    {
        PuzzleCompletedPanel.SetActive(visible);
        if(visible)
        {
            CameraController.LockInput();
        }
        else
        {
            CameraController.FreeInput();
        }
    }
}
