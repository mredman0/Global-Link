using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DailyPuzzlesPage : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if(DailyPuzzleManager.Instance.PuzzlesAreReady)
        {
            OnDailyPuzzlesReady();
        }
        DailyPuzzleManager.Instance.DailyPuzzlesReady += OnDailyPuzzlesReady;
    }

    private void OnDestroy()
    {
        DailyPuzzleManager.Instance.DailyPuzzlesReady -= OnDailyPuzzlesReady;
    }

    private void OnDailyPuzzlesReady()
    {
        var puzzles = DailyPuzzleManager.Instance.DailyPuzzles;
        var loadPuzzleButtons = GetComponentsInChildren<LoadPuzzleButton>(includeInactive: true);
        foreach(var button in loadPuzzleButtons)
        {
            button.gameObject.SetActive(false);
        }
        foreach(var kvp in puzzles)
        {
            var id = kvp.Key;
            var puzzle = kvp.Value;

            var button = loadPuzzleButtons.First(b => b.name == id);
            if(!button)
            {
                Debug.LogWarning($"Daily puzzle {id} does not have a button on the daily puzzles page");
                continue;
            }
            var available = puzzle.NodeColors != null && puzzle.NodeColors.Any();
            button.gameObject.SetActive(true);
            button.GetComponent<PuzzleLoader>().PuzzleIdInPack = puzzle.Id;
            button.GetComponent<LoadPuzzleButton>().PuzzleIdInPack = puzzle.Id;
        }
    }
}
