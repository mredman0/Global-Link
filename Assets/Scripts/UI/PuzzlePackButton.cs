using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PuzzlePackButton : MonoBehaviour
{
    [Header("Required References")]
    public TMP_Text PackNameText;
    public TMP_Text CompletedPuzzlesText;


    [Header("Settings")]
    public PackInfo Pack;

    // Start is called before the first frame update
    void Start()
    {
        PackNameText.text = Pack.Name;

        var (completed, total) = PuzzleCompletionManager.Instance.GetPackStats(Pack.Id);
        CompletedPuzzlesText.text = $"{completed} / {total}";
    }
}
