using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyPuzzlesPageButton : MonoBehaviour
{
    public Button Button;

    void Start()
    {
        Button.interactable = DailyPuzzleManager.Instance.PuzzlesAreReady;
        DailyPuzzleManager.Instance.DailyPuzzlesReady += OnDailyPuzzlesReady;
    }

    private void OnDestroy()
    {
        DailyPuzzleManager.Instance.DailyPuzzlesReady -= OnDailyPuzzlesReady;
    }

    private void OnDailyPuzzlesReady()
    {
        Button.interactable = true;
    }
}
