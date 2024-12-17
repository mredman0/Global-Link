using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class DailyPuzzleManager : MonoBehaviour
{
    public static DailyPuzzleManager Instance;

    public const string REQUEST_DATE_FORMAT = "yyyy-MM-dd";

    public event Action DailyPuzzlesReady;
    public event Action<string> DailyPuzzleFetchFailed;


    [Header("Settings")]
    public string FetchPuzzlesAddress = "https://redprismgames.com";
    public ushort FetchPuzzlesPort = 55611;
    public string FetchPuzzlesPath = "Puzzles/Daily";
    public bool UseCache = false;
    
    [Header("State")]
    public bool PuzzlesAreReady = false;
    public PuzzleConfig[] DailyPuzzles;
    public Dictionary<string, List<int>> PuzzleGroups;

    // Start is called before the first frame update
    void Start()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
#if !DEMO
        CacheDirectory = Path.Combine(Application.persistentDataPath, "DPD");

        LoadPuzzles();
#endif
    }
#if !DEMO
    private void LoadPuzzles()
    {
        if (!UseCache || !LoadCachedPuzzles())
        {
            StartCoroutine(FetchJsonCoroutine());
        }
    }

    private bool LoadCachedPuzzles()
    {
        if (!Directory.Exists(CacheDirectory) || !Directory.GetFiles(CacheDirectory).Any())
        {
            return false;
        }
        string fileToLoad = Path.Combine(CacheDirectory, $"{DateTime.Now.ToString(DateFormat)}.dat");
        if(!File.Exists(fileToLoad))
        {
            return false;
        }
        var json = File.ReadAllText(fileToLoad);
        var puzzles = JsonUtility.FromJson<PuzzlesPayload>(json);
        if(puzzles is null)
        {
            return false;
        }
        PopulateDailyPuzzles(puzzles);
        return true;
    }

    private IEnumerator FetchJsonCoroutine()
    {
        var requestDate = DateTime.Now.Date;
        var url = $"{FetchPuzzlesAddress}:{FetchPuzzlesPort}/{FetchPuzzlesPath}";
        using UnityWebRequest request = UnityWebRequest.Get(url);

        // TODO populate user-id header with unique user identifier to verify purchase state server-side
        request.SetRequestHeader("User-Id", Debug.isDebugBuild ? "PLACEHOLDER_WITH_PURCHASE" : "PLACEHOLDER");

        request.SetRequestHeader("Request-Date", DateTime.Now.ToString(REQUEST_DATE_FORMAT));

        // Send the GET request
        yield return request.SendWebRequest();

        // Check for errors
        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error fetching JSON: {request.error}");
            DailyPuzzleFetchFailed?.Invoke(request.error);
        }
        else
        {
            // Get the response
            string json = request.downloadHandler.text;

            var payload = JsonUtility.FromJson<PuzzlesPayload>(json);
            PuzzleCompletionManager.Instance.ResetDailyPuzzleCompletion();
            PopulateDailyPuzzles(payload);
            if(UseCache)
            {
                CachePuzzles(payload, requestDate);
            }
        }
    }

    private void CachePuzzles(PuzzlesPayload puzzles, DateTime date)
    {
        if(!Directory.Exists(CacheDirectory))
        {
            Directory.CreateDirectory(CacheDirectory);
        }
        else
        {
            ClearCache();
        }
        var fileName = $"{date.ToString(DateFormat)}.dat";
        var filePath = Path.Combine(CacheDirectory, fileName);
        File.WriteAllText(filePath, JsonUtility.ToJson(puzzles));
    }

    private void ClearCache()
    {
        if (!Directory.Exists(CacheDirectory))
        {
            return;
        }
        foreach(var file in Directory.EnumerateFiles(CacheDirectory))
        {
            File.Delete(file);
        }
    }

    private string CacheDirectory;
    private string DateFormat = "yyyy-MM-dd";

    private void PopulateDailyPuzzles(PuzzlesPayload payload)
    {
        if(payload is null)
        {
            return;
        }
        var orderedPayload = payload.Puzzles.OrderBy(p =>
        {
            if(!int.TryParse(p.Id, out int idInt))
            {
                return int.MaxValue;
            }
            return idInt;
        });
        DailyPuzzles = orderedPayload.Select(p => PuzzleConfigPayload.ToPuzzleConfig(p)).ToArray();
        PuzzleGroups = new Dictionary<string, List<int>>();
        foreach(var p in orderedPayload)
        {
            if(!int.TryParse(p.Id, out int idInt))
            {
                continue;
            }
            if(!PuzzleGroups.ContainsKey(p.DailyPuzzleGroup))
            {
                PuzzleGroups.Add(p.DailyPuzzleGroup, new List<int>());
            }
            PuzzleGroups[p.DailyPuzzleGroup].Add(idInt);
        }
        PuzzlesAreReady = true;
        DailyPuzzlesReady?.Invoke();
    }
#endif

    public PuzzleConfig GetPuzzle(string id)
    {
        if (!int.TryParse(id, out int idInt))
        {
            return null;
        }
        return GetPuzzle(idInt);
    }

    public PuzzleConfig GetPuzzle(int id) => id - 1 >= 0 && id - 1 < DailyPuzzles.Length ?
        DailyPuzzles[id - 1] : null;

    public bool IsPuzzleAvailable(string id)
    {
        if(!int.TryParse(id, out int idInt))
        {
            return false;
        }
        return IsPuzzleAvailable(idInt);
    }

    // Puzzle 1 is stored in index 0
    public bool IsPuzzleAvailable(int id) => id-1 >= 0 && id-1 < DailyPuzzles.Length &&
        DailyPuzzles[id-1].NodeColors != null && DailyPuzzles[id-1].NodeColors.Any();

    public string GetNextAvailablePuzzleId(string current)
    {
        if(int.TryParse(current, out int idAsInt))
        {
            idAsInt++;
            while(idAsInt <= DailyPuzzles.Length)
            {
                if(IsPuzzleAvailable(idAsInt.ToString()))
                {
                    return idAsInt.ToString();
                }
                idAsInt++;
            }
        }
        return null;
    }

    public bool AnyPuzzlesNotUnlocked() => DailyPuzzles.Any(p => !IsPuzzleAvailable(p.Id));
}

[Serializable]
public class PuzzlesPayload
{
    public PuzzleConfigPayload[] Puzzles;

    public PuzzlesPayload(List<PuzzleConfigPayload> puzzles)
    {
        Puzzles = puzzles.ToArray();
    }
}

[Serializable]
public class PuzzleConfigPayload
{
    [Header("Daily Puzzle Extra Data")]
    public string DailyPuzzleGroup;

    [Header("Metadata")]
    public string Pack;
    public string Id;

    [Header("Grid")]
    public int[] GridCellsPerRow;

    [Header("Obstacles")]
    public Vector2Int[] WallPositions;

    [Header("Nodes")]
    public Vector2Int[] NodePositions;
    public int[] NodeColors;

    [Header("Waypoints")]
    public Vector2Int[] WaypointPositions;
    public int[] WaypointColors;

    [Header("Warps")]
    public Vector2Int[] WarpPositions;

    [Header("Solutions")]
    public int[] SolutionLengths;
    public Vector2Int[] Solutions;

    [Header("View")]
    public bool OpaqueSphere;
    public Quaternion CameraArmStart;
    public float CameraDistance;
    public float CameraFoV;

    public PuzzleConfigPayload() { }

    public PuzzleConfigPayload(PuzzleConfig c)
    {
        Pack = c.Pack;
        Id = c.Id;
        GridCellsPerRow = c.GridCellsPerRow;
        WallPositions = c.WallPositions;
        NodePositions = c.NodePositions;
        NodeColors = c.NodeColors;
        WaypointPositions = c.WaypointPositions;
        WaypointColors = c.WaypointColors;
        WarpPositions = c.WarpPositions;
        SolutionLengths = c.SolutionLengths;
        Solutions = c.Solutions;
        OpaqueSphere = c.OpaqueSphere;
        CameraArmStart = c.CameraArmStart;
        CameraDistance = c.CameraDistance;
        CameraFoV = c.CameraFoV;
    }

    public static PuzzleConfig ToPuzzleConfig(PuzzleConfigPayload p)
    {
        var c = ScriptableObject.CreateInstance<PuzzleConfig>();
        c.Pack = p.Pack;
        c.Id = p.Id;
        c.GridCellsPerRow = p.GridCellsPerRow;
        c.WallPositions = p.WallPositions;
        c.NodePositions = p.NodePositions;
        c.NodeColors = p.NodeColors;
        c.WaypointPositions = p.WaypointPositions;
        c.WaypointColors = p.WaypointColors;
        c.WarpPositions = p.WarpPositions;
        c.SolutionLengths = p.SolutionLengths;
        c.Solutions = p.Solutions;
        c.OpaqueSphere = p.OpaqueSphere;
        c.CameraArmStart = p.CameraArmStart;
        c.CameraDistance = p.CameraDistance;
        c.CameraFoV = p.CameraFoV;
        return c;
    }
}
