using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class DailyPuzzleManager : MonoBehaviour
{
    public static DailyPuzzleManager Instance;

    public event Action DailyPuzzlesReady;
    public event Action<string> DailyPuzzleFetchFailed;


    [Header("Settings")]
    public string FetchPuzzlesAddress = "74.103.128.214";
    public ushort FetchPuzzlesPort = 35611;
    public bool UseCache = false;

    [Header("State")]
    public bool PuzzlesAreReady = false;
    public Dictionary<string, PuzzleConfig> DailyPuzzles;

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

        LoadPuzzles();
    }

    private const string LAST_DAILIES_DATE_KEY = "LoadedDailiesDate";
    private void LoadPuzzles()
    {
        if(!UseCache)
        {
            StartCoroutine(FetchJsonCoroutine());
            return;
        }

        var lastDailiesDate = PlayerPrefs.GetString(LAST_DAILIES_DATE_KEY, "");
        if(string.IsNullOrWhiteSpace(lastDailiesDate))
        {
            StartCoroutine(FetchJsonCoroutine());
            return;
        }
        if(DateTime.TryParse(lastDailiesDate, out DateTime date))
        {
            if(date.Date == DateTime.Now.Date)
            {
                if(!LoadCachedPuzzles())
                {
                    StartCoroutine(FetchJsonCoroutine());
                }
                return;
            }
            StartCoroutine(FetchJsonCoroutine());
            return;
        }
    }

    private bool LoadCachedPuzzles()
    {
        // TODO
        return false;
    }

    private IEnumerator FetchJsonCoroutine()
    {
        var url = $"http://{FetchPuzzlesAddress}:{FetchPuzzlesPort}";
        using UnityWebRequest request = UnityWebRequest.Get(url);

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
            Debug.Log($"Received JSON: {json}");

            var payload = JsonUtility.FromJson<PuzzlesPayload>(json);
            DailyPuzzles = new Dictionary<string, PuzzleConfig>();
            foreach(var config in payload.Puzzles.Select(p => PuzzleConfigPayload.ToPuzzleConfig(p)))
            {
                DailyPuzzles.Add(config.Id, config);
            }
            PuzzleCompletionManager.Instance.ResetDailyPuzzleCompletion();
            PuzzlesAreReady = true;
            DailyPuzzlesReady?.Invoke();
        }
    }

    public bool IsPuzzleAvailable(string id) =>
        DailyPuzzles.ContainsKey(id) && DailyPuzzles[id].NodeColors != null && DailyPuzzles[id].NodeColors.Any();

    public string GetNextAvailablePuzzleId(string current)
    {
        if(int.TryParse(current, out int idAsInt))
        {
            idAsInt++;
            while(idAsInt <= DailyPuzzles.Count)
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
}

[Serializable]
public class PuzzlesPayload
{
    public PuzzleConfigPayload[] Puzzles;

    public PuzzlesPayload(List<PuzzleConfig> puzzles)
    {
        Puzzles = puzzles.Select(c => new PuzzleConfigPayload(c)).ToArray();
    }
}

[Serializable]
public class PuzzleConfigPayload
{
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
