using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [Header("Required References")]
    public Toggle AudioMuteToggle;
    public Incrementor AudioVolumeIncrementor;
    public Toggle ControlsInvertFreeLookToggle;
    public Toggle ControlsInvertDrawingToggle;
    public Incrementor ControlsSensitivityIncrementor;
    public Toggle ColorLabelsToggle;

    public ConfirmationDialog ResetProgressConfirmDialog;

    // Start is called before the first frame update
    void Start()
    {
        LoadMute();
        LoadVolume();
        LoadInvertFreeLook();
        LoadInvertDrawing();
        LoadSensitivity();
        LoadColorLabels();

        if (Puzzle.Current)
        {
            Puzzle.Current.LockInput();
            Puzzle.Current.CameraController.LockInput();
        }

        InputManager.Instance.AddBackAction(this, HideSettings);
    }

    private void OnDestroy()
    {
        InputManager.Instance.RemoveBackAction(this);
    }

    public void HideSettings()
    {
        if(Puzzle.Current)
        {
            Puzzle.Current.FreeInput();
            Puzzle.Current.CameraController.FreeInput();
        }
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }

	#region Mute
	private const string MUTE_SETTING_KEY = "AudioMute";
    private void LoadMute()
    {
        AudioMuteToggle.SetIsOnWithoutNotify(SettingsManager.Instance.GetBool(MUTE_SETTING_KEY));
    }
    public void SetMute(bool mute)
    {
        SettingsManager.Instance.SetBool(MUTE_SETTING_KEY, mute);
    }
    #endregion

    #region Volume
    private const string VOLUME_SETTING_KEY = "AudioVolume";
    private void LoadVolume()
    {
        AudioVolumeIncrementor.SetValueWithoutNotify(SettingsManager.Instance.GetFloat(VOLUME_SETTING_KEY));
    }
    public void SetVolume(float vol)
    {
        SettingsManager.Instance.SetFloat(VOLUME_SETTING_KEY, vol);
    }
    #endregion


    #region Invert Free Look
    private const string INVERT_FREE_LOOK_SETTING_KEY = "ControlsInvertFreeLook";
    private void LoadInvertFreeLook()
    {
        ControlsInvertFreeLookToggle.SetIsOnWithoutNotify(SettingsManager.Instance.GetBool(INVERT_FREE_LOOK_SETTING_KEY));
    }
    public void SetInvertFreeLook(bool inverted)
    {
        SettingsManager.Instance.SetBool(INVERT_FREE_LOOK_SETTING_KEY, inverted);
    }
    #endregion

    #region Invert Drawing
    private const string INVERT_DRAWING_SETTING_KEY = "ControlsInvertDrawing";
    private void LoadInvertDrawing()
    {
        ControlsInvertDrawingToggle.SetIsOnWithoutNotify(SettingsManager.Instance.GetBool(INVERT_DRAWING_SETTING_KEY));
    }
    public void SetInvertDrawing(bool inverted)
    {
        SettingsManager.Instance.SetBool(INVERT_DRAWING_SETTING_KEY, inverted);
    }
    #endregion

    #region Look Sensitivity
    private const string SENSITIVITY_KEY = "ControlsSensitivity";
    private void LoadSensitivity()
    {
        ControlsSensitivityIncrementor.SetValueWithoutNotify(SettingsManager.Instance.GetFloat(SENSITIVITY_KEY));
    }
    public void SetSensitivity(float sensitivity)
    {
        SettingsManager.Instance.SetFloat(SENSITIVITY_KEY, sensitivity);
    }
    #endregion


    #region Color Labels
    private const string COLOR_LABEL_SETTING_KEY = "AccessibilityShowColorIcons";
    private void LoadColorLabels()
    {
        ColorLabelsToggle.SetIsOnWithoutNotify(SettingsManager.Instance.GetBool(COLOR_LABEL_SETTING_KEY));
    }
    public void SetColorLabels(bool show)
    {
        SettingsManager.Instance.SetBool(COLOR_LABEL_SETTING_KEY, show);
    }
    #endregion


    #region Progress Reset
    public void RequestResetAllProgress()
    {
        ResetProgressConfirmDialog.Show("Are you sure you want to reset progress?", confirm: ResetAllProgress);
    }

    public void ResetAllProgress()
    {
        if (!PuzzleCompletionManager.Instance)
        {
            return;
        }
        PuzzleCompletionManager.Instance.ResetAllProgress();

        var loadPuzzleButtons = FindObjectsByType<LoadPuzzleButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var loadPuzzleButton in loadPuzzleButtons)
        {
            loadPuzzleButton.SetButtonSpriteBasedOnCompletion();
        }
    }
    #endregion
}
