using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameplayDebugUI : MonoBehaviour
{
    public Puzzle Puzzle;
    public PuzzleProvider PuzzleProvider;

    public TMP_Dropdown ColorMapDropdown;
    public TMP_Dropdown PuzzleDropdown;
    public List<PuzzleConfig> PuzzleDropdownOptions;

    // Start is called before the first frame update
    void Start()
    {
        ResetUI();
    }

    public void ResetUI()
    {
        ColorMapDropdown.ClearOptions();
        ColorMapDropdown.AddOptions(new List<string>(ColorMapController.ColorMaps.Keys));
        int index = 0;
        foreach(var option in ColorMapDropdown.options)
        {
            if(option.text == UserSettings.Instance.SelectedColorMap)
            {
                ColorMapDropdown.SetValueWithoutNotify(index);
            }
            index++;
        }
    }

    public void LoadPuzzle()
    {
        PuzzleProvider.PuzzleConfig = PuzzleDropdownOptions[PuzzleDropdown.value];
        Puzzle.InitializePuzzle();
    }

	#region UI Event Handlers
    public void ColorMapChanged()
    {
        UserSettings.Instance.SetColorMap(ColorMapDropdown.options[ColorMapDropdown.value].text);
    }
	#endregion
}
