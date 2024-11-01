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
    public string PackId;
    public string PackName;

    // Start is called before the first frame update
    void Start()
    {
        PackNameText.text = PackName;

        var (completed, total) = PuzzleCompletionManager.Instance.GetPackStats(PackId);
        CompletedPuzzlesText.text = $"{completed} / {total}";
    }
}
