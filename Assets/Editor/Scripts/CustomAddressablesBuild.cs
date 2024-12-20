using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEditor.AddressableAssets.Build;

public class CustomAddressablesBuild
{
    public static void ConfigureAddressables(bool isDedicatedServer, bool isDemo)
    {
        var excludeLabels = new List<string>();
        excludeLabels.Add(isDedicatedServer ? "Exclude-On-Server" : "Exclude-On-Client");
        if(isDemo)
        {
            excludeLabels.Add("Exclude-On-Demo");
        }

        var defaultGroupName = isDedicatedServer ? "Default Server" : "Default Client";

        // Get the Addressable settings object
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        var defaultGroup = settings.FindGroup(defaultGroupName);
        if (defaultGroup is null)
        {
            Debug.LogError($"Could not find default group by name: {defaultGroupName}. Addressables build may not work correctly");
        }
        else
        {
            settings.DefaultGroup = defaultGroup;
            EditorUtility.SetDirty(settings);
        }

        // Iterate over all groups
        foreach (var group in settings.groups)
        {
            if (group == null) continue;

            // Check if the group has the includeLabel
            bool shouldInclude = true;
            foreach (var entry in group.entries)
            {
                foreach (var label in entry.labels)
                {
                    if (excludeLabels.Contains(label))
                    {
                        shouldInclude = false;
                        break;
                    }
                }
                if (!shouldInclude)
                {
                    break;
                }
            }

            BundledAssetGroupSchema bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
            if (bundledAssetGroupSchema != null && bundledAssetGroupSchema.IncludeInBuild != shouldInclude)
            {
                bundledAssetGroupSchema.IncludeInBuild = shouldInclude;
                EditorUtility.SetDirty(group);
            }
        }
    }

    public static AddressablesPlayerBuildResult ConfigureAndBuildAddressables(bool isDedicatedServer, bool isDemo)
    {
        ConfigureAddressables(isDedicatedServer, isDemo);
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        return result;
    }
}