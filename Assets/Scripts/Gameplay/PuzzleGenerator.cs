using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PuzzleGenerator : MonoBehaviour
{
    public GameObject PathPrefab;
    public GameObject NodePrefab;
    public GameObject VertexPrefab;

    [Header("Vertices")]
    public int NumVertices = 4000;
    public float VertexSize = 0.015f;

    private List<Vertex> Vertices = new List<Vertex>();
    private Dictionary<Vertex, bool> OccupiedVertices = new Dictionary<Vertex, bool>();

    [Header("Debug")]
    public bool ShowVertices = true;

    public float DebugStartLat;
    public float DebugStartLon;
    public float DebugGoalLat;
    public float DebugGoalLon;
    public float DebugPathStepSize = 0.02f;
    public bool DebugDrawPath = false;

    // Start is called before the first frame update
    void Start()
    {
        //CreateVertices();
    }

    #region Initialization
    public void CreateVertices()
    {
        var points = GetVertexPositions(NumVertices);
        var pointNum = 0;
        foreach (var point in points)
        {
            var vert = Instantiate(VertexPrefab);
            vert.name = $"Vertex {pointNum}";
            vert.transform.position = point;
            vert.transform.localScale = new Vector3(VertexSize, VertexSize, VertexSize);
            Vertices.Add(vert.GetComponent<Vertex>());
            pointNum++;
        }

        var startTime = Time.realtimeSinceStartup;
        foreach (var vertex in Vertices)
        {
            vertex.DetermineAdjacency(Vertices);
        }
        var endTime = Time.realtimeSinceStartup;
        Debug.Log($"Determining adjacency for {NumVertices} vertices took {endTime - startTime} seconds");

        int verticesWithNonCommutativeAdjacency = 0;
        foreach (var vertex in Vertices)
        {
            vertex.EnsureCommutativeAdjacency();
            if (!vertex.AssertCommutativeAdjacency())
            {
                verticesWithNonCommutativeAdjacency++;
            }
        }
        if (verticesWithNonCommutativeAdjacency > 0)
        {
            Debug.LogWarning($"{verticesWithNonCommutativeAdjacency} vertices have non-commutative adjacency, this could be a problem!");
        }
        else
        {
            Debug.Log($"No vertices have non-commutative adjacency, all good!");
        }

        foreach (var vertex in Vertices)
        {
            OccupiedVertices.Add(vertex, false);
        }
    }

    public static Vector3[] GetVertexPositions(int n)
    {
        if (n <= 2 || n % 2 != 0)
        {
            Debug.LogError("n must be a positive even integer greater than 2.");
            return null;
        }

        Vector3[] vertices = new Vector3[n];
        var phi = Mathf.PI * (Mathf.Sqrt(5f) - 1);

        for (int i = 0; i < n; i++)
        {
            var y = 1 - (i / (float)(n - 1)) * 2;
            var radius = Mathf.Sqrt(1 - y * y);

            var theta = phi * i;

            var x = Mathf.Cos(theta) * radius;
            var z = Mathf.Sin(theta) * radius;

            vertices[i] = new Vector3(x, y, z);
        }

        return vertices;
    }
    #endregion

    #region Pathfinding V1
    public List<Vertex> FindShortestPath(Vertex from, Vertex to)
    {
        if (OccupiedVertices[from] || OccupiedVertices[to])
        {
            return null; // Either start or end vertex is occupied
        }

        var openSet = new HashSet<Vertex>();
        var cameFrom = new Dictionary<Vertex, Vertex>();
        var gScore = new Dictionary<Vertex, float>();
        var fScore = new Dictionary<Vertex, float>();

        foreach (var vertex in Vertices)
        {
            gScore[vertex] = float.MaxValue; // Cost from start to this vertex
            fScore[vertex] = float.MaxValue; // Total cost from start to goal
        }

        gScore[from] = 0;
        fScore[from] = from.GetDistance(to); // Heuristic cost

        openSet.Add(from);

        while (openSet.Count > 0)
        {
            // Get the vertex in openSet with the lowest fScore
            var current = openSet.OrderBy(v => fScore.ContainsKey(v) ? fScore[v] : float.MaxValue).First();

            if (current == to)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);

            foreach (var neighbor in current.Adjacent)
            {
                if (OccupiedVertices[neighbor])
                {
                    continue; // Skip occupied vertices
                }

                float tentativeGScore = gScore[current] + current.GetDistance(neighbor);

                if (tentativeGScore < gScore[neighbor])
                {
                    // This path is the best until now, record it
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + neighbor.GetDistance(to);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null; // No path found
    }

    private List<Vertex> ReconstructPath(Dictionary<Vertex, Vertex> cameFrom, Vertex current)
    {
        var totalPath = new List<Vertex> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Add(current);
        }
        totalPath.Reverse();
        return totalPath;
    }
    #endregion

    #region Pathfinding V2
    public GameObject SphericalPointPrefab;
    public List<Obstacle> obstacles = new List<Obstacle>();

    public List<SphericalPoint> FindPath(SphericalPoint start, SphericalPoint goal, float stepSize)
    {
        var path = new List<SphericalPoint>();
        var current = start;

        while (current.GetDistance(goal) > stepSize)
        {
            path.Add(current);

            // Attempt to find the next point avoiding obstacles
            current = GetNextPoint(current, goal, stepSize);
            if (current == null) return null; // No valid path found
        }

        path.Add(goal); // Add the goal point
        return path;
    }

    private SphericalPoint GetNextPoint(SphericalPoint current, SphericalPoint goal, float stepSize)
    {
        // Generate a potential next point in the direction of the goal
        float newLatitude = Mathf.Lerp(current.Latitude, goal.Latitude, stepSize / current.GetDistance(goal));
        float newLongitude = Mathf.Lerp(current.Longitude, goal.Longitude, stepSize / current.GetDistance(goal) / current.Latitude);

        var nextPoint = MakeSphericalPoint(newLatitude, newLongitude);

        // Check for collisions with all obstacles
        foreach (var obstacle in obstacles)
        {
            if (obstacle.IsColliding(nextPoint))
            {
                // If colliding, find a new direction
                return AvoidObstacle(current, goal, obstacle);
            }
        }

        return nextPoint; // Valid next point
    }

    private SphericalPoint AvoidObstacle(SphericalPoint current, SphericalPoint goal, Obstacle obstacle)
    {
        // Implement a basic avoidance strategy by slightly adjusting the next point
        // This is a simplistic approach; more sophisticated methods can be used

        // Create a random offset to avoid the obstacle
        float randomOffset = 10f; // Adjust as needed

        float newLatitude = current.Latitude + Random.Range(-randomOffset, randomOffset);
        float newLongitude = current.Longitude + Random.Range(-randomOffset, randomOffset);

        // Clamp values to valid ranges
        newLatitude = Mathf.Clamp(newLatitude, -90f, 90f);
        newLongitude = (newLongitude + 180f) % 360f - 180f; // Wrap around longitude

        return MakeSphericalPoint(newLatitude, newLongitude);
    }

    private SphericalPoint MakeSphericalPoint(float lat, float lon)
    {
        var obj = Instantiate(SphericalPointPrefab);
        var point = obj.GetComponent<SphericalPoint>();
        point.Latitude = lat;
        point.Longitude = lon;
        obj.transform.position = point.ToCartesian();
        obj.transform.localScale = new Vector3(VertexSize, VertexSize, VertexSize);
        return point;
    }
    #endregion

    #region Debug
    private void OnValidate()
    {
        HandleDebugDrawPath();
    }

    private void HandleDebugDrawPath()
    {
        if (Application.isPlaying && DebugDrawPath)
        {
            DebugDrawPath = false;
            var start = MakeSphericalPoint(DebugStartLat, DebugStartLon);
            var end = MakeSphericalPoint(DebugGoalLat, DebugGoalLon);
            var path = FindPath(start, end, DebugPathStepSize);

            if(path is null)
            {
                Debug.LogWarning("No path found between given points");
                return;
            }

            for(int i = 0; i < path.Count-1; i++)
            {
                var p1 = path[i];
                var p2 = path[i + 1];
                Debug.DrawLine(p1.transform.position, p2.transform.position, color: Color.green, duration: 10f, depthTest: false);
            }

            foreach(var point in path)
            {
                if(point != start && point != end)
                {
                    Destroy(point.gameObject);
                }
            }
        }
    }
    #endregion
}
