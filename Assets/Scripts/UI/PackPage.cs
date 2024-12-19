using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

public class PackPage : MonoBehaviour
{
    public TMP_Text TitleText;
    public LocalizeStringEvent PackNameLoc;

#if DEMO
    void OnEnable()
    {
        var buttons = GetComponentsInChildren<LoadPuzzleButton>(includeInactive: true);
        foreach(var button in buttons)
        {
            if(button.PuzzlePack == "Tutorial")
            {
                break;
            }
            var id = button.PuzzleIdInPack;
            if(!int.TryParse(id, out int idInt))
            {
                button.gameObject.SetActive(false);
            }
            
            var numToInclude = (button.PuzzlePack.Contains("Expert") || button.PuzzlePack.Contains("Grandmaster")) ? 1 : 3;
            button.gameObject.SetActive(idInt <= numToInclude);
        }
    }
#endif
}
