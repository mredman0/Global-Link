using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using Newtonsoft.Json;

public class ExportPackGenConfigs : EditorWindow
{
    [MenuItem("Tools/Export Pack Gen Configs")]
    public static void ExportConfigs()
    {
        // Find all assets of type PackGenerationConfig
        string[] guids = AssetDatabase.FindAssets("t:PackGenerationConfig");
        List<PackGenerationConfig> configList = new List<PackGenerationConfig>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PackGenerationConfig asset = AssetDatabase.LoadAssetAtPath<PackGenerationConfig>(path);
            if (asset != null)
            {
                configList.Add(asset);
            }
        }

        // Create an anonymous object containing the configs
        var jsonObject = new
        {
            Configs = configList
        };

        // Convert to JSON
        string json = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

        // Show the JSON in a dialog window
        ShowJsonInDialog(json);
    }

    private static void ShowJsonInDialog(string json)
    {
        // Create a window to display the JSON
        ExportPackGenConfigs window = CreateInstance<ExportPackGenConfigs>();
        window.titleContent = new GUIContent("Pack Gen Configs JSON");
        window.jsonContent = json;
        window.minSize = new Vector2(600, 400);
        window.ShowUtility();
    }

    private string jsonContent;

    private void OnGUI()
    {
        // Display the JSON in a scrollable text area
        EditorGUILayout.LabelField("Exported JSON:", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(jsonContent, GUILayout.ExpandHeight(true));

        // Add a button to copy the JSON to the clipboard
        if (GUILayout.Button("Copy to Clipboard"))
        {
            EditorGUIUtility.systemCopyBuffer = jsonContent;
            Debug.Log("JSON copied to clipboard.");
        }

        // Add a button to save the JSON to a file
        if (GUILayout.Button("Save to File"))
        {
            string path = EditorUtility.SaveFilePanel("Save JSON to File", Application.dataPath, "PackGenConfigs.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, jsonContent);
                Debug.Log($"JSON saved to: {path}");
            }
        }
    }
}