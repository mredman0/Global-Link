using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PuzzlePackButton : MonoBehaviour
{
    [Header("Required References")]
    public TMP_Text CompletedPuzzlesText;
    public Image PackCompleteCheckmark;


    [Header("Settings")]
    public PackInfo Pack;

    // Start is called before the first frame update
    void Start()
    {
        var (completed, total) = PuzzleCompletionManager.Instance.GetPackStats(Pack.Id);
        CompletedPuzzlesText.text = $"{completed} / {total}";

        PackCompleteCheckmark.gameObject.SetActive(completed >= total);
    }
}
