using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleCompleteDialog : Dialog
{
    [Header("Required References")]
    public Puzzle Puzzle;
    public CameraController PuzzleCameraController;
    public PuzzleLoader NextLevelLoader;
    public List<GameObject> ShowWithNextLevel;
    public List<GameObject> ShowWithNoNextLevel;

    private void Start()
    {
        Puzzle.PuzzleCompleted += OnPuzzleCompleted;
        gameObject.SetActive(false);

        var nextLevel = GetNextLevelIfExists();
        if (nextLevel is null)
        {
            foreach(var obj in ShowWithNoNextLevel)
            {
                obj.SetActive(true);
            }
            foreach (var obj in ShowWithNextLevel)
            {
                obj.SetActive(false);
            }
        }
        else
        {
            foreach (var obj in ShowWithNextLevel)
            {
                obj.SetActive(true);
            }
            foreach (var obj in ShowWithNoNextLevel)
            {
                obj.SetActive(false);
            }

            NextLevelLoader.PuzzlePack = nextLevel.Value.pack;
            NextLevelLoader.PuzzleIdInPack = nextLevel.Value.idInPack;
        }
    }

    private void OnDestroy()
    {
        Puzzle.PuzzleCompleted -= OnPuzzleCompleted;
    }

    private void OnPuzzleCompleted()
    {
        Show();
    }

    protected override void OnShown()
    {
        Puzzle.LockInput();
        PuzzleCameraController.LockInput();
    }
    protected override void OnHidden()
    {
        Puzzle.FreeInput();
        PuzzleCameraController.FreeInput();
    }


    private (string pack, string idInPack)? GetNextLevelIfExists()
    {
        var currentPuzzleId = PuzzleProvider.Instance.PuzzleConfig.ID;
        var puzzleIdSplit = currentPuzzleId.Split('_');
        if (puzzleIdSplit.Length != 2)
        {
            return null;
        }
        var puzzlePack = puzzleIdSplit[0];
        var puzzleIdInPack = puzzleIdSplit[1];
        var idIsInt = int.TryParse(puzzleIdInPack, out int idInPackInt);
        if (!idIsInt)
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
}
