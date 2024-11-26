#if SERVER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class DailyPuzzleHttpServer : MonoBehaviour
{
    private HttpListener httpListener;
    private Thread serverThread;
    private bool isRunning;

    void Start()
    {
        StartServer();
    }

    void OnDestroy()
    {
        StopServer();
    }

    public void StartServer()
    {
        if (isRunning)
        {
            Debug.LogWarning("Server is already running.");
            return;
        }

        // Create and configure the HTTP listener
        httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:35611/"); // Change port as needed
        isRunning = true;

        // Start the server thread
        serverThread = new Thread(HandleRequests);
        serverThread.Start();
        Debug.Log("HTTP server started on http://localhost:35611/");
    }

    public void StopServer()
    {
        if (!isRunning) return;

        isRunning = false;
        httpListener.Stop();
        httpListener.Close();

        if (serverThread != null && serverThread.IsAlive)
            serverThread.Join();

        Debug.Log("HTTP server stopped.");
    }

    private void HandleRequests()
    {
        httpListener.Start();

        while (isRunning)
        {
            try
            {
                // Wait for an incoming request
                HttpListenerContext context = httpListener.GetContext();
                ProcessRequest(context);
            }
            catch (HttpListenerException ex)
            {
                if (isRunning)
                    Debug.LogError("HTTP Listener exception: " + ex.Message);
            }
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        // Get the request
        HttpListenerRequest request = context.Request;

        // Prepare the response
        HttpListenerResponse response = context.Response;
        response.ContentType = "application/json";
        response.StatusCode = 200;

        // JSON data to return
        var puzzleList = DailyPuzzleGenManager.Instance.GetDailyPuzzles(dateTime: DateTime.Now, new HashSet<int>());
        string responseData = JsonUtility.ToJson(new PuzzlesPayload(puzzleList), prettyPrint: true);

        // Write the response
        byte[] buffer = Encoding.UTF8.GetBytes(responseData);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();

        Debug.Log($"Handled request: {request.HttpMethod} {request.RawUrl}");
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
}
#endif