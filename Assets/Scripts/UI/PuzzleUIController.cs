using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PuzzleUIController : MonoBehaviour
{
    [Header("Required References")]
    public Puzzle Puzzle;
    public TMP_Text PuzzlePackText;
    public TMP_Text PuzzleIdInPackText;
    public GameObject NextPuzzleButton;
    public PuzzleLoader NextLevelLoader;
    public Button UndoButton;

    public List<GameObject> HideOnPuzzleComplete = new List<GameObject>();
    public List<GameObject> ShowOnPuzzleComplete = new List<GameObject>();

    private bool HasNextLevel;

    // Start is called before the first frame update
    void Start()
    {
        var puzzleConfig = PuzzleProvider.Instance.PuzzleConfig;
        SetPackAndIdText(puzzleConfig);

        var nextLevel = GetNextLevelIfExists();
        if (nextLevel != null)
        {
            HasNextLevel = true;
            NextLevelLoader.PuzzlePack = nextLevel.Value.pack;
            NextLevelLoader.PuzzleIdInPack = nextLevel.Value.idInPack;
        }

        Puzzle.UndoAvailable += OnUndoAvailable;
        Puzzle.UndoUnavailable += OnUndoUnavailable;

        // Assume there's nothing to undo at the beginning
        OnUndoUnavailable();

        Puzzle.PuzzleCompleted += OnPuzzleCompleted;

        InputManager.Instance.AddBackAction(this, GoBack);
    }

    private void OnDestroy()
    {
        Puzzle.UndoAvailable -= OnUndoAvailable;
        Puzzle.UndoUnavailable -= OnUndoUnavailable;

        Puzzle.PuzzleCompleted -= OnPuzzleCompleted;

        InputManager.Instance.RemoveBackAction(this);
    }

    public void GoBack()
    {
        SceneManager.LoadScene("Main Menu");
    }

    private void SetPackAndIdText(PuzzleConfig cfg)
    {
        PuzzlePackText.text = cfg.Pack;
        PuzzleIdInPackText.text = cfg.Id;
    }

    private void OnUndoAvailable()
    {
        UndoButton.interactable = true;
    }

    private void OnUndoUnavailable()
    {
        UndoButton.interactable = false;
    }

    private void OnPuzzleCompleted()
    {
        foreach(var toHide in HideOnPuzzleComplete)
        {
            toHide.SetActive(false);
        }
        foreach(var toShow in ShowOnPuzzleComplete)
        {
            toShow.SetActive(true);
        }
        if(HasNextLevel)
        {
            NextPuzzleButton.SetActive(true);
        }
    }

    private (string pack, string idInPack)? GetNextLevelIfExists()
    {
        var currentPuzzle = PuzzleProvider.Instance.PuzzleConfig;
        if(!currentPuzzle)
        {
            return null;
        }
        var puzzlePack = currentPuzzle.Pack;
        var puzzleIdInPack = currentPuzzle.Id;
        var idIsInt = int.TryParse(puzzleIdInPack, out int idInPackInt);
        if (!idIsInt)
        {
            return null;
        }
        var nextLevelId = (idInPackInt + 1).ToString();
        string resourcePath = $"Puzzles/{puzzlePack}/{nextLevelId}";
        var puzzleConfig = Resources.Load<PuzzleConfig>(resourcePath);
        if (!puzzleConfig)
        {
            return null;
        }
        return (puzzlePack, nextLevelId);
    }
}
