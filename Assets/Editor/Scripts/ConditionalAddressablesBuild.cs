using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using System.Collections.Generic;

public class ConditionalAddressablesBuild
{
    public static void BuildAddressables(bool isDedicatedServer)
    {
        var excludeLabels = new List<string>();
        if(isDedicatedServer)
        {
            excludeLabels.Add("Exclude-On-Server");
        }
        else
        {
            excludeLabels.Add("Exclude-On-Client");
        }

        // Get the Addressable settings object
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        // Iterate over all groups
        foreach (var group in settings.groups)
        {
            if (group == null) continue;

            // Check if the group has the includeLabel
            bool shouldInclude = true;
            foreach (var entry in group.entries)
            {
                foreach(var label in entry.labels)
                {
                    if(excludeLabels.Contains(label))
                    {
                        shouldInclude = false;
                        break;
                    }
                }
                if(!shouldInclude)
                {
                    break;
                }
            }

            // Toggle the group's "Include In Build" setting based on the label
            group.Settings.BuildAddressablesWithPlayerBuild =
                shouldInclude ? AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer : AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        }

        // Build the Addressables content
        AddressableAssetSettings.BuildPlayerContent();
    }
}