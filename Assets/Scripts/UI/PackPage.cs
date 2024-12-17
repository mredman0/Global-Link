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
            button.gameObject.SetActive(idInt < 4);
        }
    }
#endif
}
