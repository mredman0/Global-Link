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
        httpListener.Prefixes.Add("http://*:35611/"); // Change port as needed
        isRunning = true;

        // Start the server thread
        serverThread = new Thread(HandleRequests);
        serverThread.Start();
        Debug.Log("HTTP server started on http://*:35611/");
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
#endif