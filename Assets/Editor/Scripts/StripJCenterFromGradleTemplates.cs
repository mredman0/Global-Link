using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Gradle 9 removed jcenter(). Older Android Resolver versions inject it into
/// mainTemplate.gradle on Force Resolve; strip it before every player build.
/// </summary>
public class StripJCenterFromGradleTemplates : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        Strip("Assets/Plugins/Android/mainTemplate.gradle");
        Strip("Assets/Plugins/Android/settingsTemplate.gradle");
    }

    private static void Strip(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var lines = File.ReadAllLines(path);
        var filtered = lines.Where(line => line.Trim() != "jcenter()").ToArray();
        if (filtered.Length != lines.Length)
        {
            File.WriteAllLines(path, filtered);
        }
    }
}
