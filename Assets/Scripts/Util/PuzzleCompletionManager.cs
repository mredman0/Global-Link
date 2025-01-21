using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PuzzleCompletionManager : MonoBehaviour
{
    public static PuzzleCompletionManager Instance;

    public event Action<string> PackCompletionCountUpdated;
    public event Action<int> DailyPuzzleStreakUpdated;

    public List<PackInfo> PacksToManage;

    public Dictionary<string, PackInfo> PackInfo = new Dictionary<string, PackInfo>();

    public int DailyPuzzleStreak;
    public DateTime DailyPuzzleStreakLastCompletedDay;

    private Dictionary<string, int> TotalPuzzles = new Dictionary<string, int>();
    private readonly Dictionary<string, PackPuzzleCompletionData> CompletionData = new Dictionary<string, PackPuzzleCompletionData>();
    private string CompletionFolder;
    private string DailyPuzzleStreakFilePath;

    // Start is called before the first frame update
    void Start()
    {
        if (Instance)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
        
        CompletionFolder = Path.Combine(Application.persistentDataPath, "puzzle");
        DailyPuzzleStreakFilePath = Path.Combine(Application.persistentDataPath, "DPS.dat");
        if(!Directory.Exists(CompletionFolder))
        {
            Directory.CreateDirectory(CompletionFolder);
        }
        LoadAll();
        LoadDailyPuzzleStreak();
    }

    private void OnApplicationQuit()
    {
        SaveAll();
    }

    public bool IsPackCompleted(string packId)
    {
        var (completed, total) = GetPackStats(packId);
        return completed >= total;
    }
    public (int completed, int total) GetPackStats(string packId) => (CompletionData[packId].CompletedPuzzles.Count, TotalPuzzles[packId]);

    public bool IsPuzzleCompleted(string packId, string puzzleId)
    {
        if(!CompletionData.ContainsKey(packId))
        {
            return false;
        }
        return CompletionData[packId].CompletedPuzzles.Contains(puzzleId);
    }

    public bool IsPuzzleUnlocked(string packId, string puzzleId)
    {
        if (!CompletionData.ContainsKey(packId))
        {
            return false;
        }
        if(!PackInfo[packId].LevelsLocked)
        {
            // Not following level locking system for this pack
            return true;
        }
        int puzzleIdInt;
        if (!int.TryParse(puzzleId, out puzzleIdInt))
        {
            // If it's not numerical ID, assume no "progression" so it's unlocked by default
            return true;
        }
        if(puzzleIdInt == 1)
        {
            // First puzzle in a pack is always unlocked
            return true;
        }
        
        return CompletionData[packId].CompletedPuzzles.Contains((puzzleIdInt-1).ToString());
    }

    public void SetPuzzleCompleted(string packId, string puzzleId)
    {
        if (!CompletionData.ContainsKey(packId))
        {
            CompletionData.Add(packId, new PackPuzzleCompletionData() { PackName = packId });
        }
        var wasAlreadyCompleted = CompletionData[packId].CompletedPuzzles.Contains(puzzleId);
        if (!wasAlreadyCompleted)
        {
            CompletionData[packId].CompletedPuzzles.Add(puzzleId);
        }
        SavePack(packId);
        HandlePuzzleCompletionAchievements(packId, puzzleId, wasAlreadyCompleted);
        if(!wasAlreadyCompleted)
        {
            PackCompletionCountUpdated?.Invoke(packId);
            if (packId == "Daily")
            {
                CheckForDailyPuzzleStreakIncrement();
            }
        }
    }

    private void HandlePuzzleCompletionAchievements(string packId, string puzzleId, bool wasAlreadyCompleted)
    {
        if(packId == "Daily" && !wasAlreadyCompleted)
        {
            AchievementManager.Instance.Increment(AchievementManager.Achievement.DailyNovice);
            AchievementManager.Instance.Increment(AchievementManager.Achievement.DailyAdept);
            AchievementManager.Instance.Increment(AchievementManager.Achievement.DailyGuru);
            return;
        }

        var packGrouping = packId.Replace("+", "");
        var packGroupIds = CompletionData.Keys.Where(id => id.Contains(packGrouping));
        if(packGroupIds.All(id => IsPackCompleted(id)))
        {
            if(packGrouping == "Beginner")
            {
                AchievementManager.Instance.SetCompleted(AchievementManager.Achievement.PacksBeginner);
            }
            else if (packGrouping == "Intermediate")
            {
                AchievementManager.Instance.SetCompleted(AchievementManager.Achievement.PacksIntermediate);
            }
            else if (packGrouping == "Expert")
            {
                AchievementManager.Instance.SetCompleted(AchievementManager.Achievement.PacksExpert);
            }
            else if (packGrouping == "Grandmaster")
            {
                AchievementManager.Instance.SetCompleted(AchievementManager.Achievement.PacksGrandmaster);
            }
        }
    }

    private void CheckForDailyPuzzleStreakIncrement()
    {
        var dailyPuzzleDate = DailyPuzzleManager.Instance.LoadedDate ?? DateTime.Today;
        if(CompletionData["Daily"].CompletedPuzzles.Count == 3 && DailyPuzzleStreakLastCompletedDay < dailyPuzzleDate)
        {
            // On the 3rd completed puzzle, increment and save
            DailyPuzzleStreak++;
            DailyPuzzleStreakLastCompletedDay = DailyPuzzleManager.Instance.LoadedDate ?? DateTime.Today;
            SaveDailyPuzzleStreak();
            DailyPuzzleStreakUpdated?.Invoke(DailyPuzzleStreak);
        }
    }

    public void CheckForDailyPuzzleStreakLoss()
    {
        var dailyPuzzleDate = DailyPuzzleManager.Instance.LoadedDate ?? DateTime.Today;
        if(DailyPuzzleStreakLastCompletedDay < dailyPuzzleDate.AddDays(-1))
        {
            if (CompletionData["Daily"].CompletedPuzzles.Count < 3)
            {
                DailyPuzzleStreak = 0;
                SaveDailyPuzzleStreak();
                DailyPuzzleStreakUpdated?.Invoke(DailyPuzzleStreak);
            }
            else
            {
                // In case something weird happens, update the day to today so they don't lose the streak when opening the app at a later date
                DailyPuzzleStreakLastCompletedDay = dailyPuzzleDate;
                SaveDailyPuzzleStreak();
            }
        }
    }

    public void ResetAllProgress()
    {
        foreach(var pack in PacksToManage)
        {
            if (CompletionData.ContainsKey(pack.Id))
            {
                CompletionData[pack.Id].CompletedPuzzles.Clear();
                PackCompletionCountUpdated?.Invoke(pack.Id);
            }
        }
        SaveAll();
    }

    public void ResetDailyPuzzleCompletion()
    {
        if (CompletionData.ContainsKey("Daily"))
        {
            CompletionData["Daily"].CompletedPuzzles.Clear();
            PackCompletionCountUpdated?.Invoke("Daily");
        }
        SaveAll();
    }

    private void LoadAll()
    {
        if(!Directory.Exists(CompletionFolder))
        {
            return;
        }
        foreach(var pack in PacksToManage)
        {
            LoadPack(pack.Id);
        }
    }

    private void LoadPack(string pack)
    {
        PackInfo[pack] = PacksToManage.First(p => p.Id == pack);

        // Get total puzzles
        TotalPuzzles[pack] = 0;
        try
        {
            TotalPuzzles[pack] = PackInfo[pack].NumLevels;
        }
        catch { }
#if DEMO
        if(pack != "Tutorial")
        {
            var numToInclude = (pack.Contains("Expert") || pack.Contains("Grandmaster")) ? 1 : 3;
            TotalPuzzles[pack] = Mathf.Min(TotalPuzzles[pack], numToInclude);
        }
#endif

        // Get completed puzzles
        var path = Path.Combine(CompletionFolder, $"{pack}.dat");
        if (!File.Exists(path))
        {
            CompletionData[pack] = new PackPuzzleCompletionData()
            {
                PackName = pack
            };
            return;
        }
        var json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<PackPuzzleCompletionData>(json);
        if (data != null)
        {
            CompletionData[pack] = data;
        }
        PackCompletionCountUpdated?.Invoke(pack);
    }

    private void SaveAll()
    {
        foreach(var pack in PacksToManage)
        {
            SavePack(pack.Id);
        }
    }

    private void SavePack(string pack)
    {
        if (CompletionData.ContainsKey(pack))
        {
            var path = Path.Combine(CompletionFolder, $"{pack}.dat");
            var output = JsonUtility.ToJson(CompletionData[pack]);
            File.WriteAllText(path, output);
        }
    }

    private const string DATE_FORMAT = "yyyy-MM-dd";
    private void LoadDailyPuzzleStreak()
    {
        if(!File.Exists(DailyPuzzleStreakFilePath))
        {
            DailyPuzzleStreak = 0;
            DailyPuzzleStreakLastCompletedDay = DateTime.MinValue;
            return;
        }
        var fileText = File.ReadAllText(DailyPuzzleStreakFilePath);
        (DailyPuzzleStreak, DailyPuzzleStreakLastCompletedDay) = DecodeStreak(fileText);
        DailyPuzzleStreakUpdated?.Invoke(DailyPuzzleStreak);
    }
    private static (int, DateTime) DecodeStreak(string data)
    {
        try
        {
            var base64EncodedBytes = Convert.FromBase64String(data);
            var decoded = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            var parts = decoded.Split(',');
            return (int.Parse(parts[0]), DateTime.ParseExact(parts[1], DATE_FORMAT, null));
        }
        catch(Exception e)
        {
            Debug.LogException(e);
            return (0, DateTime.MinValue);
        }
    }

    private void SaveDailyPuzzleStreak()
    {
        File.WriteAllText(DailyPuzzleStreakFilePath, EncodeStreak(DailyPuzzleStreak, DailyPuzzleStreakLastCompletedDay));
    }
    private static string EncodeStreak(int streak, DateTime puzzleDate)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes($"{streak.ToString()},{puzzleDate.ToString(DATE_FORMAT)}");
        return Convert.ToBase64String(plainTextBytes);
    }

    [Serializable]
    private class PackPuzzleCompletionData
    {
        public string PackName;
        public List<string> CompletedPuzzles = new List<string>();
    }
}
