using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PackPageGenerator : EditorWindow
{
    private GameObject prefabRoot;
    private PackInfo pack;
    private Color packTint = Color.white;

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
        packTint = EditorGUILayout.ColorField("Pack Tint", packTint);

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
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, localPath);

        var page = prefab.GetComponent<PackPage>();
        var title = page.TitleText;
        title.text = pack.Name;
        title.color = packTint;

        var loadPuzzleButtons = page.GetComponentsInChildren<LoadPuzzleButton>(includeInactive: true);
        foreach(var puzzleButton in loadPuzzleButtons)
        {
            puzzleButton.PuzzlePack = pack.Id;
            puzzleButton.GetComponent<Image>().color = packTint;
        }

        PrefabUtility.SaveAsPrefabAsset(prefab, localPath);
        return prefab;
    }
}
