using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;

public static class DevUtil
{
    [MenuItem("Tools/External Tools/ADB")]
    public static void OpenADBCommandPrompt()
    {
        // Get the path to Unity's installed Android SDK folder
        string androidSdkPath = GetAndroidSdkPath();

        if (string.IsNullOrEmpty(androidSdkPath) || !Directory.Exists(androidSdkPath))
        {
            UnityEngine.Debug.LogError("Android SDK path is not set or invalid. Please check your Unity preferences.");
            return;
        }

        // Path to ADB executable
        string adbPath = Path.Combine(androidSdkPath, "platform-tools");

        if (!Directory.Exists(adbPath))
        {
            UnityEngine.Debug.LogError("ADB not found in the Android SDK folder. Please ensure you have the Android SDK installed properly.");
            return;
        }

        // Open a command prompt at the ADB location
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/K \"cd /D {adbPath}\"",
            UseShellExecute = true,
            CreateNoWindow = false
        };

        Process.Start(processInfo);
    }

    public static string GetAndroidSdkPath()
    {
        // Fetch the SDK path using Unity's internal API
        string sdkPath = EditorPrefs.GetString("AndroidSdkRoot");

        // If the preference isn't set, check Unity's internal fallback
#if UNITY_2022_1_OR_NEWER
        if (string.IsNullOrEmpty(sdkPath))
        {
            sdkPath = UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath;
        }
#endif

        return sdkPath;
    }

    public static string GetADBPath() => Path.Combine(GetAndroidSdkPath(), "platform-tools", "adb.exe");
}