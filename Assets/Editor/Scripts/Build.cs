#if ( UNITY_EDITOR )
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class Build
{
    public const string ADB_PATH = @"C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe";

    private static string[] scenes = new string[]
    {
        "Assets/_Scenes/Main Menu.unity",
        "Assets/_Scenes/Settings.unity",
        "Assets/_Scenes/Store.unity",
        "Assets/_Scenes/Puzzle.unity",
    };

    #region Build Actions
    [MenuItem("Build/Build - Windows DEV")]
    public static void BuildWindowsDev() => BuildWindows(dev: true);
    [MenuItem("Build/Build - Windows RELEASE")]
    public static void BuildWindowsRelease() => BuildWindows(dev: false);
    private static void BuildWindows(bool dev)
    {
        var buildOptions = BuildOptions.None;
        if(dev)
        {
            buildOptions |= BuildOptions.Development;
        }

        var options = new BuildPlayerOptions()
        {
            scenes = scenes,
            locationPathName = "Builds/Windows/Global Link.exe",
            target = BuildTarget.StandaloneWindows,
            options = buildOptions
        };

        _Build(options);
    }


    [MenuItem("Build/Build+Run - Android DEV")]
    public static void BuildAndRunAndroidDev() => BuildAndRunAndroid(dev: true);
    [MenuItem("Build/Build+Run - Android RELEASE")]
    public static void BuildAndRunAndroidRelease() => BuildAndRunAndroid(dev: false);
    public static void BuildAndRunAndroid(bool dev)
    {
        var buildOptions = BuildOptions.None;
        if (dev)
        {
            buildOptions |= BuildOptions.Development;
        }

        // Check for connected devices
        if (!IsDeviceConnected())
        {
            IPInputWindow.ShowWindow();
            return;
        }

        var apkPath = "Builds/Android/Global Link.apk";
        var options = new BuildPlayerOptions()
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = buildOptions
        };

        _Build(options);
        DeployToDevice(apkPath);
    }

    [MenuItem("Build/Run - Android")]
    public static void RunAndroid()
    {
        string packageName = "com.MarkRedman.GlobalLink";
        LaunchApp(packageName);
    }
	#endregion

	#region Android Helper Functions
	private static bool IsDeviceConnected()
    {
        Process process = new Process();
        process.StartInfo.FileName = ADB_PATH;
        process.StartInfo.Arguments = "devices";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output.Contains("device ") || output.Contains("device\n") || output.Contains("device\r");
    }

    private static void DeployToDevice(string apkPath)
    {
        // Make sure the APK path exists
        if (!File.Exists(apkPath))
        {
            UnityEngine.Debug.LogError("APK not found: " + apkPath);
            return;
        }

        // Execute ADB install command
        Process process = new Process();
        process.StartInfo.FileName = ADB_PATH;
        process.StartInfo.Arguments = "install -r \"" + apkPath + "\""; // -r for reinstalling
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        UnityEngine.Debug.Log("ADB Output: " + output);
        if (!string.IsNullOrEmpty(error))
        {
            UnityEngine.Debug.LogError("ADB Error: " + error);
        }
        else
        {
            UnityEngine.Debug.Log("Deployment successful!");

            string packageName = "com.MarkRedman.GlobalLink";
            LaunchApp(packageName);
        }
    }

    private static void LaunchApp(string packageName)
    {
        Process process = new Process();
        process.StartInfo.FileName = ADB_PATH;
        process.StartInfo.Arguments = $"shell am start -n {packageName}/com.unity3d.player.UnityPlayerActivity";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        UnityEngine.Debug.Log("Launch Output: " + output);
        if (!string.IsNullOrEmpty(error))
        {
            UnityEngine.Debug.LogError("Launch Error: " + error);
        }
        else
        {
            UnityEngine.Debug.Log("App launched successfully!");
        }
    }
	#endregion

	private static void _Build(BuildPlayerOptions options) => BuildPipeline.BuildPlayer(options);
}

public class IPInputWindow : EditorWindow
{
    private string ipAddress = "";

    public static void ShowWindow()
    {
        IPInputWindow window = GetWindow<IPInputWindow>("Connect to Device");
        window.minSize = new Vector2(250, 100);
    }

    private void OnGUI()
    {
        GUILayout.Label("Enter the IP address and port (e.g., 192.168.1.100:5555):", EditorStyles.wordWrappedLabel);
        ipAddress = EditorGUILayout.TextField("IP Address:", ipAddress);

        if (GUILayout.Button("Connect"))
        {
            if (!string.IsNullOrEmpty(ipAddress))
            {
                ConnectToDevice(ipAddress);
                Close();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "IP Address cannot be empty.", "OK");
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            Close();
        }
    }

    private static void ConnectToDevice(string ipAddress)
    {
        Process process = new Process();
        process.StartInfo.FileName = Build.ADB_PATH;
        process.StartInfo.Arguments = "connect " + ipAddress;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        UnityEngine.Debug.Log("Connection Output: " + output);
        if (!string.IsNullOrEmpty(error))
        {
            UnityEngine.Debug.LogError("Connection Error: " + error);
        }
        else
        {
            UnityEngine.Debug.Log("Connected to device: " + ipAddress);
        }
    }
}

#endif