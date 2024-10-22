using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsController : MonoBehaviour
{
    public ConfirmationDialog ResetProgressConfirmDialog;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void HideSettings()
    {
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }

    public void RequestResetAllProgress()
    {
        ResetProgressConfirmDialog.Show("Are you sure you want to reset progress?", confirm: ResetAllProgress);
    }

    public void ResetAllProgress()
    {
        if(!PuzzleCompletionManager.Instance)
        {
            return;
        }
        PuzzleCompletionManager.Instance.ResetAllProgress();

        var loadPuzzleButtons = FindObjectsByType<LoadPuzzleButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach(var loadPuzzleButton in loadPuzzleButtons)
        {
            loadPuzzleButton.SetButtonSpriteBasedOnCompletion();
        }
    }
}
