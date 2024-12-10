#if SERVER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class DailyPuzzleHttpServer : MonoBehaviour
{
    [Header("Settings")]
    public int Port = 35611;

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
        httpListener.Prefixes.Add($"http://*:{Port}/"); // Change port as needed
        isRunning = true;

        // Start the server thread
        serverThread = new Thread(HandleRequests);
        serverThread.Start();
        Debug.Log($"HTTP server started on http://*:{Port}/");
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

        var urlParts = request.RawUrl.Split("/", StringSplitOptions.RemoveEmptyEntries);

        if(urlParts.Length == 2 && urlParts[0] == "Puzzles" && urlParts[1] == "Daily")
        {
            ProcessDailyPuzzlesRequest(context);
        }
        else // No expected path found
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            Debug.Log($"Served 404 for request: {request.HttpMethod} {request.RawUrl}");
        }
    }

    private void ProcessDailyPuzzlesRequest(HttpListenerContext context)
    {
        // TODO do something with the user id
        var userId = context.Request.Headers.Get("User-Id") ?? "";

        // TODO fill in hash set based on verified user purchases
        var puzzleAvailabilityKeys = new HashSet<int>();
        if(userId != null && userId.Contains("WITH_PURCHASE"))
        {
            puzzleAvailabilityKeys.Add(1);
        }

        var requestedDateStr = context.Request.Headers.Get("Request-Date") ?? "";
        var couldParse = DateTime.TryParseExact(requestedDateStr, DailyPuzzleManager.REQUEST_DATE_FORMAT,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime requestedDate);
        if(!couldParse ||
            requestedDate.Date < DateTime.Today.Date.AddDays(-1) || requestedDate.Date > DateTime.Today.Date.AddDays(1))
        {
            requestedDate = DateTime.Today;
        }

        var puzzleList = DailyPuzzleGenManager.Instance.GetDailyPuzzles(dateTime: requestedDate, puzzleAvailabilityKeys);
        RespondWithJson(context, new PuzzlesPayload(puzzleList));

        var dayStr = "today's";
        if(requestedDate.Date == DateTime.Today.Date.AddDays(-1))
        {
            dayStr = "yesterday's";
        }
        else if(requestedDate.Date == DateTime.Today.Date.AddDays(1))
        {
            dayStr = "tomorrow's";
        }
        Debug.Log($"Served {dayStr} daily puzzles for {userId}");
    }

    private void RespondWithJson(HttpListenerContext context, object toSerialize)
    {
        var response = context.Response;
        response.ContentType = "application/json";
        response.StatusCode = 200;

        string responseData = JsonUtility.ToJson(toSerialize);

        // Write the response
        byte[] buffer = Encoding.UTF8.GetBytes(responseData);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }
}
#endif