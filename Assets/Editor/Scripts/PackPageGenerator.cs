#if ( UNITY_EDITOR )
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class PackPageGenerator : EditorWindow
{
    private GameObject prefabRoot;
    private PackInfo pack;

    [MenuItem("Tools/Pack Page Generator")]
    public static void ShowWindow()
    {
        GetWindow<PackPageGenerator>("Pack Page Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Prefab Generator", EditorStyles.boldLabel);

        prefabRoot = (GameObject)EditorGUILayout.ObjectField("Base Pack Page", prefabRoot, typeof(GameObject), true);

        pack = (PackInfo)EditorGUILayout.ObjectField("Pack", pack, typeof(PackInfo), true);

        if (GUILayout.Button("Generate Prefab"))
        {
            GeneratePrefab();
        }
    }

    void GeneratePrefab()
    {
        if (prefabRoot == null)
        {
            Debug.LogWarning("Please assign a prefab root object.");
            return;
        }


        // Create the prefab
        var prefab = ModifyAndResave(prefabRoot);
        if (prefab != null)
        {
            Debug.Log("Pack Page created successfully");
        }
        else
        {
            Debug.LogError("Failed to create Pack Page.");
        }
    }

    GameObject ModifyAndResave(GameObject prefabRoot)
    {
        string localPath = "Assets/Prefabs/UI/Pack Pages/" + pack.Id + " Pack Page.prefab";
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot);

        var page = instance.GetComponent<PackPage>();
        var title = page.TitleText;
        var loc = page.PackNameLoc;
        loc.StringReference = pack.Name;
        title.color = pack.Tint;

        var loadPuzzleButtons = page.GetComponentsInChildren<LoadPuzzleButton>(includeInactive: true);
        int buttons = 0;
        foreach(var puzzleButton in loadPuzzleButtons)
        {
            buttons++;
            if(buttons > pack.NumLevels)
            {
                puzzleButton.gameObject.SetActive(false);
            }
            puzzleButton.PuzzlePack = pack.Id;
            puzzleButton.GetComponent<Image>().color = pack.Tint;
        }
        buttons++;
        var buttonToClone = loadPuzzleButtons.First().gameObject;
        for(; buttons <= pack.NumLevels; buttons++)
        {
            var newButtonGO = Instantiate(buttonToClone, buttonToClone.transform.parent);
            newButtonGO.name = buttons.ToString();
            var newButton = newButtonGO.GetComponent<LoadPuzzleButton>();
            newButton.PuzzlePack = pack.Id;
            newButton.PuzzleIdInPack = buttons.ToString();
            newButton.GetComponent<Image>().color = pack.Tint;
        }
        var variant = PrefabUtility.SaveAsPrefabAsset(instance, localPath);
        DestroyImmediate(instance);
        
        return variant;
    }
}
#endif