#if ( UNITY_EDITOR )
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Build
{
    private const string ANDROID_PACKAGE_NAME = "com.redprismgames.chromasphere";
    private static string[] scenes = new string[]
    {
        "Assets/_Scenes/Init.unity",
    };

    #region Build Actions
    [MenuItem("Build/Windows/Build DEV")]
    public static void BuildWindowsDev() => BuildWindows(dev: true);
    [MenuItem("Build/Windows/Build RELEASE")]
    public static void BuildWindowsRelease() => BuildWindows(dev: false);
    [MenuItem("Build/Windows/Clean Build DEV")]
    public static void CleanBuildWindowsDev() => BuildWindows(dev: true, clean: true);
    [MenuItem("Build/Windows/Clean Build RELEASE")]
    public static void CleanBuildWindowsRelease() => BuildWindows(dev: false, clean: true);
    private static void BuildWindows(bool dev, bool clean = false)
    {
        SelectWindows(); // Make sure we're in Windows Standalone player mode

        var buildOptions = BuildOptions.None;
        if(dev)
        {
            buildOptions |= BuildOptions.Development;
        }

        var options = new BuildPlayerOptions()
        {
            scenes = scenes,
            locationPathName = "Builds/Windows/ChromaSphere.exe",
            target = BuildTarget.StandaloneWindows,
            options = buildOptions
        };

        _Build(options, isDedicatedServer: false, clean);
    }


    [MenuItem("Build/Server/Build DEV")]
    public static void BuildServerDev() => BuildServer(dev: true);
    [MenuItem("Build/Server/Build RELEASE")]
    public static void BuildServerRelease() => BuildServer(dev: false);
    [MenuItem("Build/Server/Clean Build DEV")]
    public static void CleanBuildServerDev() => BuildServer(dev: true, clean: true);
    [MenuItem("Build/Server/Clean Build RELEASE")]
    public static void CleanBuildServerRelease() => BuildServer(dev: false, clean: true);
    private static void BuildServer(bool dev, bool clean = false)
    {
        SelectDedicatedServer(); // Make sure we're in Dedicated Server player mode

        var buildOptions = BuildOptions.None;
        if (dev)
        {
            buildOptions |= BuildOptions.Development;
        }

        var options = new BuildPlayerOptions()
        {
            scenes = scenes,
            locationPathName = "Builds/Server/ChromaSphere_Server.exe",
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = buildOptions
        };

        var success = _Build(options, isDedicatedServer: true, clean);
        if(success)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(Directory.GetCurrentDirectory(), "Builds/Server/"),
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }


	#region Android Actions
	[MenuItem("Build/Android/Build DEV")]
    public static void BuildAndroidDev() => BuildAndRunAndroid(dev: true, clean: false, buildAppBundle: true);
    [MenuItem("Build/Android/Build RELEASE")]
    public static void BuildAndroidRelease() => BuildAndRunAndroid(dev: false, clean: false, buildAppBundle: true);
    [MenuItem("Build/Android/Build+Run DEV")]
    public static void BuildAndRunAndroidDev() => BuildAndRunAndroid(dev: true, clean: false, runAfterBuild: true, buildAppBundle: false);
    [MenuItem("Build/Android/Build+Run RELEASE")]
    public static void BuildAndRunAndroidRelease() => BuildAndRunAndroid(dev: false, clean: false, runAfterBuild: true, buildAppBundle: false);
    [MenuItem("Build/Android/Clean Build DEV")]
    public static void CleanBuildAndroidDev() => BuildAndRunAndroid(dev: true, clean: true, buildAppBundle: true);
    [MenuItem("Build/Android/Clean Build RELEASE")]
    public static void CleanBuildAndroidRelease() => BuildAndRunAndroid(dev: false, clean: true, buildAppBundle: true);
    [MenuItem("Build/Android/Clean Build+Run DEV")]
    public static void CleanBuildAndRunAndroidDev() => BuildAndRunAndroid(dev: true, clean: true, runAfterBuild: true, buildAppBundle: false);
    [MenuItem("Build/Android/Clean Build+Run RELEASE")]
    public static void CleanBuildAndRunAndroidRelease() => BuildAndRunAndroid(dev: false, clean: true, runAfterBuild: true, buildAppBundle: false);
    #endregion

    #region Android Demo Actions
    [MenuItem("Build/Android DEMO/Build DEV")]
    public static void BuildAndroidDemoDev() => BuildAndRunAndroid(dev: true, clean: false, buildAppBundle: true, demo: true);
    [MenuItem("Build/Android DEMO/Build RELEASE")]
    public static void BuildAndroidDemoRelease() => BuildAndRunAndroid(dev: false, clean: false, buildAppBundle: true, demo: true);
    [MenuItem("Build/Android DEMO/Build+Run DEV")]
    public static void BuildAndRunAndroidDemoDev() => BuildAndRunAndroid(dev: true, clean: false, runAfterBuild: true, buildAppBundle: false, demo: true);
    [MenuItem("Build/Android DEMO/Build+Run RELEASE")]
    public static void BuildAndRunAndroidDemoRelease() => BuildAndRunAndroid(dev: false, clean: false, runAfterBuild: true, buildAppBundle: false, demo: true);
    [MenuItem("Build/Android DEMO/Clean Build DEV")]
    public static void CleanBuildAndroidDemoDev() => BuildAndRunAndroid(dev: true, clean: true, buildAppBundle: true, demo: true);
    [MenuItem("Build/Android DEMO/Clean Build RELEASE")]
    public static void CleanBuildAndroidDemoRelease() => BuildAndRunAndroid(dev: false, clean: true, buildAppBundle: true, demo: true);
    [MenuItem("Build/Android DEMO/Clean Build+Run DEV")]
    public static void CleanBuildAndRunAndroidDemoDev() => BuildAndRunAndroid(dev: true, clean: true, runAfterBuild: true, buildAppBundle: false, demo: true);
    [MenuItem("Build/Android DEMO/Clean Build+Run RELEASE")]
    public static void CleanBuildAndRunAndroidDemoRelease() => BuildAndRunAndroid(dev: false, clean: true, runAfterBuild: true, buildAppBundle: false, demo: true);
    #endregion

    public static void BuildAndRunAndroid(bool dev, bool clean = false, bool runAfterBuild = false, bool buildAppBundle = false, bool demo = false)
    {
        if (runAfterBuild && !Android_IsDeviceConnected())
        {
            Debug.LogError("No Android device detected. Connect a device first");
            return;
        }

        SelectAndroid(); // Make sure we're in Android player mode
        
        var scriptingDefines = new List<string>();
        if(demo)
        {
            scriptingDefines.Add("DEMO");
        }

        var buildOptions = BuildOptions.None;
        if (dev)
        {
            buildOptions |= BuildOptions.Development;
        }

        EditorUserBuildSettings.buildAppBundle = buildAppBundle;
        EditorUserBuildSettings.androidCreateSymbols = buildAppBundle ? AndroidCreateSymbols.Public : AndroidCreateSymbols.Disabled;
        PlayerSettings.Android.splitApplicationBinary = buildAppBundle;

        var extension = buildAppBundle ? "aab" : "apk";
        var apkPath = $"Builds/Android/ChromaSphere.{extension}";
        var options = new BuildPlayerOptions()
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = buildOptions,
            extraScriptingDefines = scriptingDefines.ToArray()
        };

        PlayerSettings.Android.keystorePass = System.Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYSTORE_PASS");
        PlayerSettings.Android.keyaliasPass = System.Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYSTORE_ALIAS_PASS");

        if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
        {
            Debug.LogError("Please make sure UNITY_ANDROID_KEYSTORE_PASS and UNITY_ANDROID_KEYSTORE_ALIAS_PASS are defined!");
            // Reset player settings to standard values to prevent version control churn
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
            PlayerSettings.Android.splitApplicationBinary = true;
            return;
        }

        var success = _Build(options, isDedicatedServer: false, clean);

        // Reset player settings to standard values to prevent version control churn
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
        PlayerSettings.Android.splitApplicationBinary = true;

        if (!success)
        {
            return;
        }
        if (buildAppBundle && !dev)
        {
            PlayerSettings.Android.bundleVersionCode++;
            PlayerSettings.iOS.buildNumber = (int.Parse(PlayerSettings.iOS.buildNumber)+1).ToString();
            Debug.Log($"Android bundle version code incremented to {PlayerSettings.Android.bundleVersionCode}");
            PlayerSettings.bundleVersion = IncrementVersionNumber(PlayerSettings.bundleVersion);
            Debug.Log($"Version number incremented to {PlayerSettings.bundleVersion}");
        }
        if (runAfterBuild)
        {
            success = Android_DeployToDevice(apkPath);
            if (!success)
            {
                return;
            }
            Android_LaunchApp(ANDROID_PACKAGE_NAME);
        }
    }


    [MenuItem("Build/Android/Redeploy+Run")]
    public static void RedeployAndRunAndroid()
    {
        if (!Android_IsDeviceConnected())
        {
            Debug.LogError("No Android device detected. Connect a device first");
            return;
        }
        var apkPath = $"Builds/Android/ChromaSphere.apk";
        var success = Android_DeployToDevice(apkPath);
        if(!success)
        {
            return;
        }
        Android_LaunchApp(ANDROID_PACKAGE_NAME);
    }

    [MenuItem("Build/Android/Run")]
    public static void RunAndroid()
    {
        if (!Android_IsDeviceConnected())
        {
            Debug.LogError("No Android device detected. Connect a device first");
            return;
        }
        Android_LaunchApp(ANDROID_PACKAGE_NAME);
    }
	#endregion

	#region Android Helper Functions
	private static bool Android_IsDeviceConnected()
    {
        Process process = new Process();
        process.StartInfo.FileName = DevUtil.GetADBPath();
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

    private static bool Android_DeployToDevice(string apkPath)
    {
        // Make sure the APK path exists
        if (!File.Exists(apkPath))
        {
            Debug.LogError("APK not found: " + apkPath);
            return false;
        }

        // Execute ADB install command
        Process process = new Process();
        process.StartInfo.FileName = DevUtil.GetADBPath();
        process.StartInfo.Arguments = "install -r \"" + apkPath + "\""; // -r for reinstalling
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Debug.Log("ADB Output: " + output);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError("ADB Error: " + error);
            return false;
        }

        Debug.Log("Deployment successful!");
        return true;
    }

    private static void Android_LaunchApp(string packageName)
    {
        Process process = new Process();
        process.StartInfo.FileName = DevUtil.GetADBPath();
        process.StartInfo.Arguments = $"shell am start -n {packageName}/com.unity3d.player.UnityPlayerActivity";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Debug.Log("Launch Output: " + output);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError("Launch Error: " + error);
        }
        else
        {
            Debug.Log("App launched successfully!");
        }
    }
    #endregion


    #region iOS Actions
    [MenuItem("Build/iOS/Build DEV")]
    public static void BuildiOSDev() => BuildiOS(dev: true, clean: false);
    [MenuItem("Build/iOS/Build RELEASE")]
    public static void BuildiOSRelease() => BuildiOS(dev: false, clean: false);
    [MenuItem("Build/iOS/Clean Build DEV")]
    public static void CleanBuildiOSDev() => BuildiOS(dev: true, clean: true);
    [MenuItem("Build/iOS/Clean Build RELEASE")]
    public static void CleanBuildiOSRelease() => BuildiOS(dev: false, clean: true);
    #endregion

    public static void BuildiOS(bool dev, bool clean = false)
    {
        SelectiOS(); // Make sure we're in iOS player mode

        var scriptingDefines = new List<string>();

        var buildOptions = BuildOptions.None;
        if (dev)
        {
            buildOptions |= BuildOptions.Development;
        }

        var outputPath = $"Builds/iOS/ChromaSphere";
        var options = new BuildPlayerOptions()
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.iOS,
            options = buildOptions,
            extraScriptingDefines = scriptingDefines.ToArray()
        };

        var success = _Build(options, isDedicatedServer: false, clean);
        if (!success)
        {
            return;
        }
    }

    #region Build Player Target Actions
    private const string PLAYER_MENU_PATH = "Build/_Player/";

    [MenuItem(PLAYER_MENU_PATH + "Dedicated Server")]
    public static void SelectDedicatedServer()
    {
        SetBuildTarget(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, true);
    }

    [MenuItem(PLAYER_MENU_PATH + "Windows")]
    public static void SelectWindows()
    {
        SetBuildTarget(BuildTarget.StandaloneWindows, BuildTargetGroup.Standalone, false);
    }

    [MenuItem(PLAYER_MENU_PATH + "Android")]
    public static void SelectAndroid()
    {
        SetBuildTarget(BuildTarget.Android, BuildTargetGroup.Android);
    }

    [MenuItem(PLAYER_MENU_PATH + "iOS")]
    public static void SelectiOS()
    {
        SetBuildTarget(BuildTarget.iOS, BuildTargetGroup.iOS);
    }

    // Helper method to set build target and build group
    private static void SetBuildTarget(BuildTarget target, BuildTargetGroup group, bool isServer = false)
    {
        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
        }

        // Handle Dedicated Server specifics (if applicable)
        if (isServer)
        {
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
        }
        else
        {
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
        }
    }

    // Validate menu options to show a checkmark if the target is active
    [MenuItem(PLAYER_MENU_PATH + "Dedicated Server", true)]
    public static bool ValidateDedicatedServer()
    {
        return ValidateBuildTarget(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, true);
    }

    [MenuItem(PLAYER_MENU_PATH + "Windows", true)]
    public static bool ValidateWindows()
    {
        return ValidateBuildTarget(BuildTarget.StandaloneWindows, BuildTargetGroup.Standalone, false);
    }

    [MenuItem(PLAYER_MENU_PATH + "Android", true)]
    public static bool ValidateAndroid()
    {
        return ValidateBuildTarget(BuildTarget.Android, BuildTargetGroup.Android);
    }

    [MenuItem(PLAYER_MENU_PATH + "iOS", true)]
    public static bool ValidateiOS()
    {
        return ValidateBuildTarget(BuildTarget.iOS, BuildTargetGroup.iOS);
    }

    // Helper method to validate the current target
    private static bool ValidateBuildTarget(BuildTarget target, BuildTargetGroup group, bool isServer = false)
    {
        bool isActive = EditorUserBuildSettings.activeBuildTarget == target;

        if (target == BuildTarget.StandaloneWindows64 && isServer)
        {
            isActive &= EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server;
        }

        Menu.SetChecked(PLAYER_MENU_PATH + (isServer ? "Dedicated Server" : target.ToString()), isActive);
        return true; // Always return true to keep the menu enabled
    }
    #endregion

    private static bool _Build(BuildPlayerOptions options, bool isDedicatedServer, bool cleanBuild)
    {
        if (cleanBuild)
        {
            options.options |= BuildOptions.CleanBuildCache;

            var pathParts = options.locationPathName.Split('/');
            var directory = Path.Combine(pathParts.Take(pathParts.Length - 1).ToArray());
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
            Directory.CreateDirectory(directory);
        }
        var result = CustomAddressablesBuild.ConfigureAndBuildAddressables(isDedicatedServer, options.extraScriptingDefines.Contains("DEMO"));
        if(!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError($"Addressables Build Failed");
            Debug.LogError(result.Error);
            Debug.LogError($"Not continuing with build as addressables build failed");
            return false;
        }
        var report = BuildPipeline.BuildPlayer(options);
        return report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
    }

	#region Version Numbering
    public static string IncrementVersionNumber(string current)
    {
        var parts = current.Split('.');
        var major = int.Parse(parts[0]);
        var minor = int.Parse(parts[1]);
        var build = int.Parse(parts[2]);
        return $"{major}.{minor}.{build+1}";
    }
	#endregion
}

public class IPInputWindow : EditorWindow
{
    private string ipAddress = "192.168.1.";

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
        process.StartInfo.FileName = DevUtil.GetADBPath();
        process.StartInfo.Arguments = "connect " + ipAddress;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Debug.Log("Connection Output: " + output);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError("Connection Error: " + error);
        }
        else
        {
            Debug.Log("Connected to device: " + ipAddress);
        }
    }
}

#endif