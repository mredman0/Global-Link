using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PuzzleCompletionManager : MonoBehaviour
{
    public static PuzzleCompletionManager Instance;

    public List<string> PacksToManage;
    public int ExpectedTutorialLevels = 6;

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

    public bool IsPuzzleCompleted(string puzzleId)
    {
        var packAndId = GetPuzzlePackAndId(puzzleId);
        if(packAndId is null)
        {
            return false;
        }
        if(!CompletionData.ContainsKey(packAndId.Value.pack))
        {
            return false;
        }
        return CompletionData[packAndId.Value.pack].CompletedPuzzles.Contains(packAndId.Value.idInPack);
    }

    public bool IsTutorialComplete()
    {
        if(!CompletionData.ContainsKey("Tutorial"))
        {
            return false;
        }
        return CompletionData["Tutorial"].CompletedPuzzles.Count >= ExpectedTutorialLevels;
    }

    public void SetPuzzleCompleted(string puzzleId)
    {
        var packAndId = GetPuzzlePackAndId(puzzleId);
        if (packAndId is null)
        {
            return;
        }
        var pack = packAndId.Value.pack;
        if (!CompletionData.ContainsKey(pack))
        {
            CompletionData.Add(pack, new PackPuzzleCompletionData() { PackName = pack });
        }
        if(!CompletionData[pack].CompletedPuzzles.Contains(packAndId.Value.idInPack))
        {
            CompletionData[pack].CompletedPuzzles.Add(packAndId.Value.idInPack);
        }
        SavePack(pack);
    }

    public void ResetAllProgress()
    {
        foreach(var pack in PacksToManage)
        {
            if (CompletionData.ContainsKey(pack))
            {
                CompletionData[pack].CompletedPuzzles.Clear();
            }
        }
        SaveAll();
    }

    private (string pack, string idInPack)? GetPuzzlePackAndId(string puzzleId)
    {
        var idSplit = puzzleId.Split('_');
        if (idSplit.Length != 2)
        {
            return null;
        }
        return (idSplit[0], idSplit[1]);
    }

    private void LoadAll()
    {
        if(!Directory.Exists(CompletionFolder))
        {
            return;
        }
        foreach(var pack in PacksToManage)
        {
            LoadPack(pack);
        }
    }

    private void LoadPack(string pack)
    {
        // Get completed puzzles
        var path = Path.Combine(CompletionFolder, $"{pack}.dat");
        if (!File.Exists(path))
        {
            return;
        }
        var json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<PackPuzzleCompletionData>(json);
        if (data != null)
        {
            CompletionData[pack] = data;
        }

        // Get total puzzles
        int puzzleNum = 1;
        while(Resources.Load($"Puzzles/{pack}/{pack}_{puzzleNum}"))
        {
            puzzleNum++;
        }
        TotalPuzzles[pack] = puzzleNum - 1;
    }

    private void SaveAll()
    {
        foreach(var pack in PacksToManage)
        {
            SavePack(pack);
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
