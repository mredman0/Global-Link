using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PuzzleCompletionManager : MonoBehaviour
{
    public static PuzzleCompletionManager Instance;

    public List<PackInfo> PacksToManage;

    public Dictionary<string, PackInfo> PackInfo = new Dictionary<string, PackInfo>();
    private Dictionary<string, int> TotalPuzzles = new Dictionary<string, int>();
    private readonly Dictionary<string, PackPuzzleCompletionData> CompletionData = new Dictionary<string, PackPuzzleCompletionData>();
    private string CompletionFolder;

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
        if(!Directory.Exists(CompletionFolder))
        {
            Directory.CreateDirectory(CompletionFolder);
        }
        LoadAll();
    }

    private void OnApplicationQuit()
    {
        SaveAll();
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

    public void SetPuzzleCompleted(string packId, string puzzleId)
    {
        if (!CompletionData.ContainsKey(packId))
        {
            CompletionData.Add(packId, new PackPuzzleCompletionData() { PackName = packId });
        }
        if(!CompletionData[packId].CompletedPuzzles.Contains(puzzleId))
        {
            CompletionData[packId].CompletedPuzzles.Add(puzzleId);
        }
        SavePack(packId);
    }

    public void ResetAllProgress()
    {
        foreach(var pack in PacksToManage)
        {
            if (CompletionData.ContainsKey(pack.Id))
            {
                CompletionData[pack.Id].CompletedPuzzles.Clear();
            }
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
        int puzzleNum = 1;
        while (Resources.Load($"Puzzles/{pack}/{puzzleNum}"))
        {
            puzzleNum++;
        }
        TotalPuzzles[pack] = puzzleNum - 1;

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

    [Serializable]
    private class PackPuzzleCompletionData
    {
        public string PackName;
        public List<string> CompletedPuzzles = new List<string>();
    }
}
