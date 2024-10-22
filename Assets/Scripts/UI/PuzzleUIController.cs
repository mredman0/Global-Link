using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PuzzleUIController : MonoBehaviour
{
    [Header("Required References")]
    public Puzzle Puzzle;
    public TMP_Text PuzzlePackText;
    public TMP_Text PuzzleIdInPackText;


    // Start is called before the first frame update
    void Start()
    {
        var puzzleConfig = PuzzleProvider.Instance.PuzzleConfig;
        SetPackAndIdText(puzzleConfig);
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
}
