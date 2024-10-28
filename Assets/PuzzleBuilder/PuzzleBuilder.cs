#if ( UNITY_EDITOR )
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class PuzzleBuilder : MonoBehaviour
{
    [Header("Required References")]
    public PuzzleGrid Grid;
    public Camera Camera;
    public PuzzleBuilderCameraController CameraController;

    [Header("Prefabs")]
    public GameObject NodePrefab;
    public GameObject WaypointPrefab;
    public GameObject WarpPrefab;
    public GameObject WallPrefab;
    public GameObject RockPrefab;

    [Header("Config")]
    public int[] GridCellsPerRow = new int[2] { 4, 4 };

    [Header("State")]
    public PuzzleBuilderPaintMode PaintMode = PuzzleBuilderPaintMode.Node;
    [Range(0, 5)]
    public int PaintNodeColor = 0;
    public Dictionary<GridCell, PuzzleObject> PlacedObjects = new Dictionary<GridCell, PuzzleObject>();
    public List<PuzzleObjectNode> Nodes = new List<PuzzleObjectNode>();
    public List<PuzzleObjectWaypoint> Waypoints = new List<PuzzleObjectWaypoint>();
    public List<PuzzleObjectWarp> Warps = new List<PuzzleObjectWarp>();
    public PuzzleObjectWarp LastPlacedWarp;
    public List<PuzzleObjectWall> Walls = new List<PuzzleObjectWall>();
    public List<PuzzleObjectRock> Rocks = new List<PuzzleObjectRock>();

    [Header("View Settings")]
    public bool OpaqueSphere;
    public Quaternion CameraArmStart;
    public bool SnapCameraArmStartToCurrent;
    public bool SnapCameraArmToStart;

    public float CameraDistance;
    public float CameraFoV;
    public bool SnapToCameraSettings;

    [Header("Generator")]
    public string GeneratorSeed = "";
    public string PreviousSeed;
    [Range(0, 1)]
    public float InitialWallAmount = 0f;
    [Range(0, 1)]
    [Tooltip("0 means each wall is selected individually,\n0.25 means walls will be grouped into ~4 clusters,\n0.5 means ~2 clusters,\n1 means a single cluster")]
    public float WallClustering = 0f;
    [Range(1, 6)]
    public int TargetNodePairs = 6;
    [Range(0, 6)]
    public int TargetWaypoints = 0;
    public int MinimumDistanceBetweenNodes = 4;
    [Range(0, 1)]
    public float AdditionalWallAmount = 0f;
    public bool StartGeneration = false;

    [Header("Saving")]
    public string PuzzleName = "Puzzle";
    public bool SavePuzzle = false;

    [Header("Loading")]
    public PuzzleConfig ConfigToLoad;
    public bool LoadPuzzle = false;

    [Header("Debug")]
    public bool DoGridRebuild = false;


    private static int MAIN_SPHERE_LAYER_MASK;
    // Start is called before the first frame update
    void Start()
    {
        MAIN_SPHERE_LAYER_MASK = LayerMask.GetMask("InputCatch");
        RebuildGrid();
        InputManager.Instance.Tap += OnTap;
    }

    private void OnTap(Vector2 tapPosition)
    {
        var ray = Camera.ScreenPointToRay(tapPosition);
        var anyHit = Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, MAIN_SPHERE_LAYER_MASK);
        if (anyHit)
        {
            var hitPoint = hitInfo.point;
            GridCell closestCell = null;
            float closestCellDistance = float.MaxValue;
            foreach (var cell in Grid.Cells)
            {
                var dist = Vector3.Distance(cell.transform.position, hitPoint);
                if (dist < closestCellDistance)
                {
                    closestCell = cell;
                    closestCellDistance = dist;
                }
            }
            if (closestCell is null)
            {
                Debug.LogWarning("Could not find cell to paint to");
                return;
            }
            Paint(closestCell);
        }
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(1)) // Right click
        {
            var ray = Camera.ScreenPointToRay(Input.mousePosition);
            var anyHit = Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, MAIN_SPHERE_LAYER_MASK);
            if (anyHit)
            {
                var hitPoint = hitInfo.point;
                GridCell closestCell = null;
                float closestCellDistance = float.MaxValue;
                foreach (var cell in Grid.Cells)
                {
                    var dist = Vector3.Distance(cell.transform.position, hitPoint);
                    if (dist < closestCellDistance)
                    {
                        closestCell = cell;
                        closestCellDistance = dist;
                    }
                }
                if (closestCell is null)
                {
                    Debug.LogWarning("Could not find cell to paint to");
                    return;
                }
                if(!PlacedObjects.ContainsKey(closestCell) || !(PlacedObjects[closestCell] is PuzzleObjectNode))
                {
                    ColorCell(closestCell);
                }
            }
        }

        HandleActionBools();
    }

    private void HandleActionBools()
    {
        if (SavePuzzle)
        {
            SavePuzzle = false;
            Save(PuzzleName);
        }
        if (LoadPuzzle)
        {
            LoadPuzzle = false;
            Load(ConfigToLoad);
        }
        if (DoGridRebuild)
        {
            DoGridRebuild = false;
            Clear();
            RebuildGrid();
        }
        if (StartGeneration)
        {
            StartGeneration = false;
            Clear();
            GeneratePuzzle();
        }
        if(SnapCameraArmStartToCurrent)
        {
            SnapCameraArmStartToCurrent = false;
            CameraArmStart = CameraController.CameraArm.transform.rotation;
        }
        if(SnapCameraArmToStart)
        {
            SnapCameraArmToStart = false;
            CameraController.SnapTo(CameraArmStart, CameraController.Camera.transform.position.magnitude, CameraController.Camera.fieldOfView);
        }
        if(SnapToCameraSettings)
        {
            SnapToCameraSettings = false;
            CameraController.SnapTo(CameraController.CameraArm.transform.rotation, CameraDistance, CameraFoV);
        }
    }

	#region Operations
	public void Paint(GridCell cell)
    {
        if(PlacedObjects.ContainsKey(cell))
        {
            EraseObject(cell);
        }
        if(PaintMode == PuzzleBuilderPaintMode.Erase)
        {
            return;
        }

        if(PaintMode == PuzzleBuilderPaintMode.Node)
        {
            var newNodeGO = Instantiate(NodePrefab);
            newNodeGO.transform.parent = transform;
            newNodeGO.transform.position = cell.transform.position;

            var newNode = newNodeGO.GetComponent<PuzzleObjectNode>();
            newNodeGO.name = $"{ColorMapController.Instance.ColorName(PaintNodeColor)} Node";
            newNode.SetColor(PaintNodeColor);
            newNode.Cell = cell;

            if(cell.Color != PaintNodeColor)
            {
                ColorCell(cell);
            }

            Nodes.Add(newNode);
            PlacedObjects[cell] = newNode;
        }
        else if(PaintMode == PuzzleBuilderPaintMode.Waypoint)
        {
            var newWaypointGO = Instantiate(WaypointPrefab);
            newWaypointGO.transform.parent = transform;
            newWaypointGO.transform.position = cell.transform.position;
            newWaypointGO.transform.LookAt(transform);

            var newWaypoint = newWaypointGO.GetComponent<PuzzleObjectWaypoint>();
            newWaypointGO.name = $"{ColorMapController.Instance.ColorName(PaintNodeColor)} Waypoint";
            newWaypoint.SetColor(PaintNodeColor);
            newWaypoint.Cell = cell;

            if (cell.Color != PaintNodeColor)
            {
                ColorCell(cell);
            }

            Waypoints.Add(newWaypoint);
            PlacedObjects[cell] = newWaypoint;
        }
        else if(PaintMode == PuzzleBuilderPaintMode.Warp)
        {
            var newWarpGO = Instantiate(WarpPrefab);
            newWarpGO.transform.parent = transform;
            newWarpGO.transform.position = cell.transform.position;
            newWarpGO.transform.LookAt(transform);

            var newWarp = newWarpGO.GetComponent<PuzzleObjectWarp>();
            newWarpGO.name = $"Warp r{cell.Row}c{cell.Cell}";
            newWarp.Cell = cell;

            if(LastPlacedWarp && !LastPlacedWarp.PairedWarp)
            {
                LastPlacedWarp.SetPairedWarp(newWarp);
                newWarp.SetPairedWarp(LastPlacedWarp);
            }
            LastPlacedWarp = newWarp;

            Warps.Add(newWarp);
            PlacedObjects[cell] = newWarp;
        }
        else if (PaintMode == PuzzleBuilderPaintMode.Wall)
        {
            var newWallGO = Instantiate(WallPrefab);
            newWallGO.transform.parent = transform;
            newWallGO.transform.position = cell.transform.position;
            newWallGO.transform.LookAt(transform);

            var newWall = newWallGO.GetComponent<PuzzleObjectWall>();
            newWall.Cell = cell;
            ColorCell(cell, -1);

            Walls.Add(newWall);
            PlacedObjects[cell] = newWall;
        }
        else if(PaintMode == PuzzleBuilderPaintMode.Rock)
        {
            var newRockGO = Instantiate(RockPrefab);
            newRockGO.transform.parent = transform;
            newRockGO.transform.position = cell.transform.position;
            newRockGO.transform.LookAt(transform);

            var newRock = newRockGO.GetComponent<PuzzleObjectRock>();
            newRock.Cell = cell;
            ColorCell(cell, -1);

            Rocks.Add(newRock);
            PlacedObjects[cell] = newRock;
        }
    }

    private void EraseObject(GridCell cell)
    {
        var objectToDelete = PlacedObjects[cell];

        if(objectToDelete is PuzzleObjectNode node)
        {
            Nodes.Remove(node);
            ColorCell(cell, -1);
        }
        else if (objectToDelete is PuzzleObjectWaypoint waypoint)
        {
            Waypoints.Remove(waypoint);
        }
        else if (objectToDelete is PuzzleObjectWarp warp)
        {
            Warps.Remove(warp);
            if(warp.PairedWarp)
            {
                warp.Unpair();
            }
        }
        else if (objectToDelete is PuzzleObjectWall wall)
        {
            Walls.Remove(wall);
        }
        else if(objectToDelete is PuzzleObjectRock rock)
        {
            Rocks.Remove(rock);
        }

        Destroy(objectToDelete.gameObject);
        PlacedObjects.Remove(cell);
    }

    private void ColorCell(GridCell cell)
    {
        if(cell.Color == PaintNodeColor)
        {
            ColorCell(cell, -1);
        }
        else
        {
            ColorCell(cell, PaintNodeColor);
        }
    }

    private Color Gray = Color.gray;
    private void ColorCell(GridCell cell, int? color)
    {
        var col = color is null || color == -1 ? Gray : ColorMapController.Instance.ApplyActiveColorMap(color.Value);
        Grid.IntersectionBallsByCell[cell].material.SetColor("_Color", col);
        cell.Color = color;

        if(color == -1)
        {
            // Grayify all neighbors
            foreach(var kvp in Grid.CellConnectionDetails)
            {
                if(kvp.Value.Item1 == cell || kvp.Value.Item2 == cell)
                {
                    kvp.Key.material.SetColor("_Color", col);
                }
            }
        }

        foreach(var neighbor in cell.Neighbors)
        {
            // Colorify lines if the corresponding neighbors are also our color
            foreach(var kvp in Grid.CellConnectionDetails)
            {
                if(kvp.Value.Item1 == cell)
                {
                    if(kvp.Value.Item2.Color == color)
                    {
                        kvp.Key.material.SetColor("_Color", col);
                    }
                    else
                    {
                        kvp.Key.material.SetColor("_Color", Gray);
                    }
                }
                else if(kvp.Value.Item2 == cell)
                {
                    if (kvp.Value.Item1.Color == color)
                    {
                        kvp.Key.material.SetColor("_Color", col);
                    }
                    else
                    {
                        kvp.Key.material.SetColor("_Color", Gray);
                    }
                }
            }
        }
    }
	#endregion

	#region Generator
    public void GeneratePuzzle()
    {
        SetRandomSeed();

        GenerateInitialWalls();
        GenerateNodesPathsAndWaypoints();
        GenerateAdditionalWalls();
    }

    private void SetRandomSeed()
    {
        int seed = string.IsNullOrEmpty(GeneratorSeed) ? (int)DateTime.Now.Ticks : GeneratorSeed.GetHashCode();
        if (int.TryParse(GeneratorSeed, out int seedNumber))
        {
            seed = seedNumber;
        }
        PreviousSeed = string.IsNullOrEmpty(GeneratorSeed) ? seed.ToString() : GeneratorSeed;
        Random.InitState(seed);
    }
	
    private void GenerateInitialWalls()
    {
        PaintMode = PuzzleBuilderPaintMode.Wall;
        var targetWalls = Mathf.RoundToInt(Grid.Cells.Count * InitialWallAmount);

        if(targetWalls < 1)
        {
            return;
        }

        if(WallClustering == 0)
        {
            var availableCells = new List<GridCell>();
            availableCells.AddRange(Grid.Cells);

            for(int i = 0; i < targetWalls; i++)
            {
                var randomIndex = Random.Range(0, availableCells.Count);
                Paint(availableCells[randomIndex]);
                availableCells.RemoveAt(randomIndex);
            }
            return;
        }

        // Chance to end cluster should be ((numInCluster / targetClusterSize) / 2) ^ 2
        var targetClusterSize = targetWalls * WallClustering;
        var wallsGenerated = 0;
        var wallsInCluster = new List<GridCell>();
        var availableNeighbors = new List<GridCell>();
        var startNewCluster = true;
        while(wallsGenerated < targetWalls)
        {
            if(startNewCluster)
            {
                availableNeighbors.Clear();
                availableNeighbors.AddRange(GetOpenCells(Grid.Cells));
                var randomIndex = Random.Range(0, availableNeighbors.Count);
                var cell = availableNeighbors[randomIndex];
                Paint(cell);
                availableNeighbors.Clear();
                availableNeighbors.AddRange(GetOpenCells(cell.Neighbors));
                wallsInCluster.Add(cell);
                wallsGenerated++;
                startNewCluster = false;
            }
            else if(!availableNeighbors.Any())
            {
                startNewCluster = true;
            }
            else if(Random.value < Mathf.Pow(((wallsInCluster.Count / targetClusterSize) / 2), 2))
            {
                startNewCluster = true;
            }
            else
            {
                var randomIndex = Random.Range(0, availableNeighbors.Count);
                var cell = availableNeighbors[randomIndex];
                Paint(cell);
                availableNeighbors.RemoveAll(c => c == cell);
                availableNeighbors.AddRange(GetOpenCells(cell.Neighbors));
                wallsInCluster.Add(cell);
                wallsGenerated++;
            }
        }
    }

    private Dictionary<int, List<GridCell>> GeneratedSolutionPaths = new Dictionary<int, List<GridCell>>();
    private void GenerateNodesPathsAndWaypoints()
    {
        PaintMode = PuzzleBuilderPaintMode.Node;
        List<List<GridCell>> contiguousGroupsOfCells;
        var colors = new List<int>();
        for(int i = 0; i < 6; i++)
        {
            colors.Add(i);
        }
        var colorOrder = new int[colors.Count];
        for(int i = 0; i < colorOrder.Length; i++)
        {
            var randInd = Random.Range(0, colors.Count);
            colorOrder[i] = colors[randInd];
            colors.RemoveAt(randInd);
        }

        var waypointsLeftToGenerate = TargetWaypoints;

        for(int i = 0; i < TargetNodePairs; i++)
        {
            PaintNodeColor = colorOrder[i];
            contiguousGroupsOfCells = GetContiguousGroupsOfCells();

            List<GridCell> biggestGroup = null;
            int biggestGroupSize = 0;
            foreach(var group in contiguousGroupsOfCells)
            {
                if(group.Count > biggestGroupSize)
                {
                    biggestGroup = group;
                    biggestGroupSize = group.Count;
                }
            }

            var generateWaypoint = waypointsLeftToGenerate > 0;

            if(biggestGroupSize < 4)
            {
                break; // Can't place any more node pairs
            }

            if(generateWaypoint)
            {
                var possibleWaypoints = biggestGroup.Where(cell => !cell.IsDeadEnd()).ToList();
                var waypoint = possibleWaypoints[Random.Range(0, possibleWaypoints.Count)];
                biggestGroup.Remove(waypoint);
                var cellsFarEnoughAway = biggestGroup.Where(cell => Grid.DistanceBetween(waypoint, cell) >= MinimumDistanceBetweenNodes).ToList();
                if (!cellsFarEnoughAway.Any())
                {
                    // We tried to prefer something further away, but just give up
                    cellsFarEnoughAway = biggestGroup;
                }
                var firstNode = cellsFarEnoughAway[Random.Range(0, cellsFarEnoughAway.Count)];
                var pathToFirstNode = Grid.GetShortestPath(waypoint, firstNode);
                foreach (var cell in pathToFirstNode)
                {
                    ColorCell(cell);
                }
                ColorCell(firstNode);
                var cellsToConsiderForSecondNode = Grid.GetContiguousCells(waypoint);
                cellsToConsiderForSecondNode.Remove(waypoint);
                cellsFarEnoughAway = cellsToConsiderForSecondNode.Where(cell => Grid.DistanceBetween(waypoint, cell) >= MinimumDistanceBetweenNodes).ToList();
                if (!cellsFarEnoughAway.Any())
                {
                    // We tried to prefer something further away, but just give up
                    cellsFarEnoughAway = cellsToConsiderForSecondNode;
                }
                var secondNode = cellsFarEnoughAway[Random.Range(0, cellsFarEnoughAway.Count)];
                var pathToSecondNode = Grid.GetShortestPath(waypoint, secondNode);
                foreach (var cell in pathToSecondNode)
                {
                    ColorCell(cell);
                }
                var completePath = new List<GridCell>();
                completePath.AddRange(pathToFirstNode);
                completePath.Reverse();
                completePath.Add(waypoint);
                completePath.AddRange(pathToSecondNode);
                GeneratedSolutionPaths[colorOrder[i]] = completePath;

                PaintMode = PuzzleBuilderPaintMode.Waypoint;
                Paint(waypoint);
                waypointsLeftToGenerate--;

                PaintMode = PuzzleBuilderPaintMode.Node;
                Paint(firstNode);
                Paint(secondNode);
            }
            else
            {
                var firstNode = biggestGroup[Random.Range(0, biggestGroup.Count)];
                biggestGroup.Remove(firstNode);
                var cellsFarEnoughAway = biggestGroup.Where(cell => Grid.DistanceBetween(firstNode, cell) >= MinimumDistanceBetweenNodes).ToList();
                if (!cellsFarEnoughAway.Any())
                {
                    // We tried to prefer something further away, but just give up
                    cellsFarEnoughAway = biggestGroup;
                }
                var secondNode = cellsFarEnoughAway[Random.Range(0, cellsFarEnoughAway.Count)];

                var path = Grid.GetShortestPath(firstNode, secondNode);
                foreach (var cell in path)
                {
                    ColorCell(cell);
                }
                GeneratedSolutionPaths[colorOrder[i]] = path;

                PaintMode = PuzzleBuilderPaintMode.Node;
                Paint(firstNode);
                Paint(secondNode);
            }
        }
        if(waypointsLeftToGenerate > 0)
        {
            Debug.LogWarning($"Only generated {TargetWaypoints - waypointsLeftToGenerate} out of the requested {TargetWaypoints} waypoints");
        }
    }

    private List<List<GridCell>> GetContiguousGroupsOfCells()
    {
        var groups = new List<List<GridCell>>();
        var ungroupedCells = GetOpenCells(Grid.Cells).ToList();
        while(ungroupedCells.Any())
        {
            var group = new List<GridCell>();
            groups.Add(group);
            group.AddRange(Grid.GetContiguousCells(ungroupedCells.First()));
            foreach(var cell in group)
            {
                ungroupedCells.Remove(cell);
            }
        }
        return groups;
    }

    private void GenerateAdditionalWalls()
    {
        PaintMode = PuzzleBuilderPaintMode.Wall;

        var availableCells = GetOpenCells(Grid.Cells).ToList();
        var targetWalls = Mathf.RoundToInt(availableCells.Count * AdditionalWallAmount);
        
        for (int i = 0; i < targetWalls; i++)
        {
            var randomIndex = Random.Range(0, availableCells.Count);
            Paint(availableCells[randomIndex]);
            availableCells.RemoveAt(randomIndex);
        }
        return;
    }

    private IEnumerable<GridCell> GetOpenCells(IEnumerable<GridCell> toConsider) => toConsider.Where(c => c.Color is null);
    #endregion

	#region Reset Functions
	public void Clear()
    {
        ClearNodes();
        ClearWaypoints();
        ClearWarps();
        GeneratedSolutionPaths.Clear();
        ClearObstacles();
        ResetGridCellColors();
    }

    public void ClearNodes()
    {
        foreach(var node in Nodes)
        {
            PlacedObjects.Remove(node.Cell);
            Destroy(node.gameObject);
        }
        Nodes.Clear();
    }
    public void ClearWaypoints()
    {
        foreach (var waypoint in Waypoints)
        {
            PlacedObjects.Remove(waypoint.Cell);
            Destroy(waypoint.gameObject);
        }
        Waypoints.Clear();
    }
    public void ClearWarps()
    {
        foreach(var warp in Warps)
        {
            warp.Unpair();
            Destroy(warp.gameObject);
        }
        Warps.Clear();
    }

    public void ClearObstacles()
    {
        foreach (var wall in Walls)
        {
            PlacedObjects.Remove(wall.Cell);
            Destroy(wall.gameObject);
        }
        Walls.Clear();
        foreach (var rock in Rocks)
        {
            PlacedObjects.Remove(rock.Cell);
            Destroy(rock);
        }
        Rocks.Clear();
    }

    private void ResetGridCellColors()
    {
        foreach(var cell in Grid.Cells)
        {
            ColorCell(cell, null);
        }
    }

    public void RebuildGrid()
    {
        Grid.Clear();
        Grid.Initialize(GridCellsPerRow, gridVisible: true);
    }
	#endregion

	#region Saving/Loading
    public void Save(string puzzleName)
    {
        if(string.IsNullOrWhiteSpace(puzzleName))
        {
            Debug.LogError("Cannot save puzzle with empty name");
            return;
        }

        var newPuzzleConfig = ScriptableObject.CreateInstance<PuzzleConfig>();

        // Metadata
        newPuzzleConfig.ID = puzzleName;

        // Grid
        newPuzzleConfig.GridCellsPerRow = GridCellsPerRow;

        // Nodes
        newPuzzleConfig.NodePositions = new Vector2Int[Nodes.Count];
        newPuzzleConfig.NodeColors = new int[Nodes.Count];
        int i = 0;
        for(int color = 0; color < 6; color++)
        {
            foreach(var node in Nodes.Where(n => n.Color == color))
            {
                newPuzzleConfig.NodePositions[i] = new Vector2Int(node.Cell.Row, node.Cell.Cell);
                newPuzzleConfig.NodeColors[i] = node.Color;
                i++;
            }
        }

        // Waypoints
        newPuzzleConfig.WaypointPositions = new Vector2Int[Waypoints.Count];
        newPuzzleConfig.WaypointColors = new int[Waypoints.Count];
        for (i = 0; i < Waypoints.Count; i++)
        {
            newPuzzleConfig.WaypointPositions[i] = new Vector2Int(Waypoints[i].Cell.Row, Waypoints[i].Cell.Cell);
            newPuzzleConfig.WaypointColors[i] = Waypoints[i].Color;
        }

        // Warps
        var warpsInPairedOrder = new List<PuzzleObjectWarp>();
        foreach(var warp in Warps)
        {
            if(!warp.PairedWarp)
            {
                Debug.LogWarning($"{warp.name} does not have a paired warp and will not be saved");
                continue;
            }
            if(!warpsInPairedOrder.Contains(warp))
            {
                warpsInPairedOrder.Add(warp);
                warpsInPairedOrder.Add(warp.PairedWarp);
            }
        }
        newPuzzleConfig.WarpPositions = new Vector2Int[warpsInPairedOrder.Count];
        for(i = 0; i < warpsInPairedOrder.Count; i++)
        {
            newPuzzleConfig.WarpPositions[i] = new Vector2Int(warpsInPairedOrder[i].Cell.Row, warpsInPairedOrder[i].Cell.Cell);
        }

        // Walls
        newPuzzleConfig.WallPositions = new Vector2Int[Walls.Count];
        for (i = 0; i < Walls.Count; i++)
        {
            newPuzzleConfig.WallPositions[i] = new Vector2Int(Walls[i].Cell.Row, Walls[i].Cell.Cell);
        }

        // Rocks
        newPuzzleConfig.RockPositions = new Vector2Int[Rocks.Count];
        for(i = 0; i < Rocks.Count; i++)
        {
            newPuzzleConfig.RockPositions[i] = new Vector2Int(Rocks[i].Cell.Row, Rocks[i].Cell.Cell);
        }

        // Solutions
        var solutions = new List<GridCell>();
        var solutionLengths = new int[6];
        for(int color = 0; color < 6; color++)
        {
            if(GeneratedSolutionPaths.ContainsKey(color))
            {
                solutions.AddRange(GeneratedSolutionPaths[color]);
                solutionLengths[color] = GeneratedSolutionPaths[color].Count;
            }
            else
            {
                // If we don't have a generated path to store, try figuring it out... dumbly
                var steps = new List<GridCell>();
                var nodes = Nodes.Where(n => n.Color == color).ToList();
                if (nodes.Count == 2)
                {
                    var start = nodes[0].Cell;
                    var end = nodes[1].Cell;
                    var current = start;
                    while (current != end)
                    {
                        var next = current.Neighbors.FirstOrDefault(n => n.Color == color && n != start && !steps.Contains(n));
                        if (!next)
                        {
                            Debug.LogWarning($"Could not follow solution path for color {color}");
                            break;
                        }
                        if (next != end)
                        {
                            steps.Add(next);
                        }
                        current = next;
                    }
                }
                else if (nodes.Any())
                {
                    Debug.LogWarning($"Color {color} does not have exactly 2 nodes, not storing solution.");
                }
                solutions.AddRange(steps);
                solutionLengths[color] = steps.Count;
            }
        }
        newPuzzleConfig.SolutionLengths = solutionLengths;
        newPuzzleConfig.Solutions = solutions.Select(c => new Vector2Int(c.Row, c.Cell)).ToArray();

        // View Settings
        newPuzzleConfig.OpaqueSphere = OpaqueSphere;
        newPuzzleConfig.CameraArmStart = CameraArmStart;
        newPuzzleConfig.CameraDistance = CameraDistance;
        newPuzzleConfig.CameraFoV = CameraFoV;

        var puzzleNameSplitByUnderscore = puzzleName.Split('_');
        if(puzzleNameSplitByUnderscore.Length == 2)
        {
            var pack = puzzleNameSplitByUnderscore[0];
            var puzzleId = puzzleNameSplitByUnderscore[1];
            if(!AssetDatabase.IsValidFolder($"Assets/Resources/Puzzles/{pack}"))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Puzzles", pack);
            }
            AssetDatabase.CreateAsset(newPuzzleConfig, $"Assets/Resources/Puzzles/{pack}/{puzzleName}.asset");
        }
        else
        {
            AssetDatabase.CreateAsset(newPuzzleConfig, $"Assets/Resources/Puzzles/{puzzleName}.asset");
        }
    }

    public void Load(PuzzleConfig cfg)
    {
        Clear();

        // Metadata
        PuzzleName = cfg.ID;

        // Grid
        GridCellsPerRow = new int[cfg.GridCellsPerRow.Length];
        cfg.GridCellsPerRow.CopyTo(GridCellsPerRow, 0);
        RebuildGrid();

        // Nodes
        if(cfg.NodePositions != null && cfg.NodeColors != null)
        {
            PaintMode = PuzzleBuilderPaintMode.Node;
            for (int i = 0; i < cfg.NodePositions.Length; i++)
            {
                var row = cfg.NodePositions[i].x;
                var rowCell = cfg.NodePositions[i].y;
                PaintNodeColor = cfg.NodeColors.Length > i ? cfg.NodeColors[i] : Mathf.FloorToInt(i / 2f);
                Paint(Grid.CellsByRow[row][rowCell]);
            }
        }

        // Waypoints
        if(cfg.WaypointPositions != null)
        {
            PaintMode = PuzzleBuilderPaintMode.Waypoint;
            for (int i = 0; i < cfg.WaypointPositions.Length; i++)
            {
                PaintNodeColor = cfg.WaypointColors[i];
                var row = cfg.WaypointPositions[i].x;
                var rowCell = cfg.WaypointPositions[i].y;
                Paint(Grid.CellsByRow[row][rowCell]);
            }
        }

        // Warps
        if(cfg.WarpPositions != null)
        {
            PaintMode = PuzzleBuilderPaintMode.Warp;
            for(int i = 0; i < cfg.WarpPositions.Length; i++)
            {
                var row = cfg.WarpPositions[i].x;
                var rowCell = cfg.WarpPositions[i].y;
                Paint(Grid.CellsByRow[row][rowCell]);
            }
        }

        // Walls
        if(cfg.WallPositions != null)
        {
            PaintMode = PuzzleBuilderPaintMode.Wall;
            for (int i = 0; i < cfg.WallPositions.Length; i++)
            {
                var row = cfg.WallPositions[i].x;
                var rowCell = cfg.WallPositions[i].y;
                Paint(Grid.CellsByRow[row][rowCell]);
            }
        }

        // Rocks
        if(cfg.RockPositions != null)
        {
            PaintMode = PuzzleBuilderPaintMode.Rock;
            for (int i = 0; i < cfg.RockPositions.Length; i++)
            {
                var row = cfg.RockPositions[i].x;
                var rowCell = cfg.RockPositions[i].y;
                Paint(Grid.CellsByRow[row][rowCell]);
            }
        }

        // Solutions
        if(cfg.SolutionLengths != null && cfg.Solutions != null)
        {
            PaintMode = PuzzleBuilderPaintMode.Node;
            var currentStep = 0;
            for (int i = 0; i < 6; i++)
            {
                var solution = new List<GridCell>();
                var lengthOfSolution = cfg.SolutionLengths[i];
                var end = currentStep + lengthOfSolution;
                PaintNodeColor = i;
                for (; currentStep < end; currentStep++)
                {
                    var row = cfg.Solutions[currentStep].x;
                    var rowCell = cfg.Solutions[currentStep].y;

                    if (Grid.CellsByRow[row][rowCell].Color != PaintNodeColor)
                    {
                        ColorCell(Grid.CellsByRow[row][rowCell]);
                    }
                    solution.Add(Grid.CellsByRow[row][rowCell]);
                }
                GeneratedSolutionPaths[i] = solution;
            }
        }

        // View Settings
        OpaqueSphere = cfg.OpaqueSphere;
        CameraArmStart = cfg.CameraArmStart;
        CameraDistance = cfg.CameraDistance;
        CameraFoV = cfg.CameraFoV;
        SnapCameraArmToStart = true;
        SnapToCameraSettings = true;
    }
	#endregion
}

public enum PuzzleBuilderPaintMode
{
    Erase,
    Node,
    Waypoint,
    Warp,
    Wall,
    Rock,
}
#endif