using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    }

    private void OnDestroy()
    {
        Puzzle.UndoAvailable -= OnUndoAvailable;
        Puzzle.UndoUnavailable -= OnUndoUnavailable;

        Puzzle.PuzzleCompleted -= OnPuzzleCompleted;
    }

    private void SetPackAndIdText(PuzzleConfig cfg)
    {
        var packAndId = GetPuzzlePackAndId(cfg);
        if(packAndId is null)
        {
            PuzzlePackText.text = "";
            PuzzleIdInPackText.text = "";
        }
        else
        {
            PuzzlePackText.text = packAndId.Value.pack;
            PuzzleIdInPackText.text = packAndId.Value.idInPack;
        }
    }

    private (string pack, string idInPack)? GetPuzzlePackAndId(PuzzleConfig cfg)
    {
        var idSplit = cfg.ID.Split('_');
        if (idSplit.Length != 2)
        {
            return null;
        }
        return (idSplit[0], idSplit[1]);
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
