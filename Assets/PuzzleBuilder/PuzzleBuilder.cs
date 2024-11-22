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
    public PuzzleGenerationMode GenerationMode;

    [Header("Warps")]
    [Range(0, 8)]
    public int TargetWarpPairs = 0;
    public int MinimumWarpDistance = 3;

    [Header("Initial Walling")]
    [Range(0, 1)]
    public float InitialWallAmount = 0f;
    [Range(0, 1)]
    [Tooltip("Higher values make initial walls prefer to be placed randomly")]
    public float InitialWallNormalness = 0f;
    [Range(0, 1)]
    [Tooltip("Higher values make initial walls prefer to be placed around many existing walls")]
    public float InitialWallClustering = 0f;
    [Range(0, 1)]
    [Tooltip("Higher values make initial walls prefer to be placed next to single other existing walls")]
    public float InitialWallNoodling = 0f;

    [Header("Nodes/Waypoints")]
    [Range(1, 6)]
    public int TargetNodePairs = 6;
    [Range(0, 6)]
    public int TargetWaypoints = 0;

    [Header("Extra Walls")]
    [Range(0, 1)]
    public float AdditionalWallAmount = 0f;

    [Header("Misc Generator Settings")]
    public int PreferredDistanceBetweenNodes = 8;
    public int MicroWaywardness = 4;
    public bool WallUnusedPlaceholders = false;

    [Header("GENERATE")]
    public bool StartGeneration = false;

    [Header("Saving")]
    public string Pack = "Pack";
    public string Id = "#";
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
            Paint(closestCell, isManual: true);
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
            Save();
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
            RebuildGrid();
            GeneratePuzzle();
        }
        if (SnapCameraArmStartToCurrent)
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
	public void Paint(GridCell cell, bool isManual = false)
    {
        if(PlacedObjects.ContainsKey(cell))
        {
            EraseObject(cell, isManual: isManual);
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
            newNodeGO.name = $"C{PaintNodeColor} Node";
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
            newWaypointGO.name = $"C{PaintNodeColor} Waypoint";
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

    private void EraseObject(GridCell cell, bool isManual)
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
                warp.Unpair(issueWarning: isManual);
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
        var col = color is null || color == -1 ? Gray : ColorManager.Instance.GetColor(color.Value);
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

        switch (GenerationMode)
        {
            case PuzzleGenerationMode.Hemispheres:
                GenerateHemispheresWall();
                GenerateHemisphereWarps();
                GenerateStandardInitialWalls();
                GenerateStandardNodesPathsAndWaypoints();
                RemoveUnusedWarps();
                GenerateStandardAdditionalWalls();
                break;
            default:
                GenerateStandardWarps();
                GenerateStandardInitialWalls();
                GenerateStandardNodesPathsAndWaypoints();
                RemoveUnusedWarps();
                GenerateStandardAdditionalWalls();
                break;
        }
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

    #region Hemispheres
    private const int HemisphereWallStepDegrees = 1;
    private void GenerateHemispheresWall()
    {
        PaintMode = PuzzleBuilderPaintMode.Wall;

        var normal = Random.onUnitSphere;
        var start = Vector3.Cross(normal, Mathf.Abs(normal.x) < Mathf.Abs(normal.z) ? Vector3.right : Vector3.up).normalized;
        var offset = Random.Range(-0.4f, 0.4f);

        for(float degrees = 0; degrees < 360f; degrees += HemisphereWallStepDegrees)
        {
            var rotation = Quaternion.AngleAxis(degrees, normal);
            var lookAt = ((rotation * start) + normal * offset).normalized;
            var cell = Grid.GetLookingAtCell(lookAt.ToPolar());
            Paint(cell);
        }
    }
    private void GenerateHemisphereWarps()
    {
        var contiguousGroups = GetContiguousGroupsOfCells();
        if(contiguousGroups.Count != 2)
        {
            Debug.LogError($"Hemisphere wall generation did not create exactly 2 contiguous groups of cells. Generating standard warps");
            GenerateStandardWarps();
            return;
        }
        var warpCells = new List<GridCell>();
        var availableCells1 = GetOpenCells(contiguousGroups[0]).ToList();
        var availableCells2 = GetOpenCells(contiguousGroups[1]).ToList();
        for (int pair = 0; pair < TargetWarpPairs; pair++)
        {
            var warp1Cell = availableCells1[Random.Range(0, availableCells1.Count)];
            availableCells1.Remove(warp1Cell);

            var warp2Cell = availableCells2[Random.Range(0, availableCells2.Count)];
            availableCells2.Remove(warp2Cell);

            warpCells.Add(warp1Cell);
            warpCells.Add(warp2Cell);
        }

        PaintMode = PuzzleBuilderPaintMode.Warp;
        for (int i = 0; i < warpCells.Count; i += 2)
        {
            Paint(warpCells[i]);
            Paint(warpCells[i + 1]);
        }

        foreach (var warp in Warps)
        {
            if (!warp.PairedWarp)
            {
                Debug.LogWarning($"Warp {warp.name} does not have a paired warp!");
            }
        }
    }
    #endregion

    #region Standard
    private void GenerateStandardWarps()
    {
        var warpCells = new List<GridCell>();
        var availableCells = GetOpenCells(Grid.Cells).ToList();
        for (int pair = 0; pair < TargetWarpPairs; pair++)
        {
            var warp1Cell = availableCells[Random.Range(0, availableCells.Count)];
            availableCells.Remove(warp1Cell);

            var cellsFarEnoughAway = availableCells.Where(cell => Grid.DistanceBetween(warp1Cell, cell) >= MinimumWarpDistance).ToList();
            var warp2Cell = cellsFarEnoughAway[Random.Range(0, cellsFarEnoughAway.Count)];
            availableCells.Remove(warp2Cell);

            warpCells.Add(warp1Cell);
            warpCells.Add(warp2Cell);
        }

        PaintMode = PuzzleBuilderPaintMode.Warp;
        for (int i = 0; i < warpCells.Count; i += 2)
        {
            Paint(warpCells[i]);
            Paint(warpCells[i + 1]);
        }

        foreach (var warp in Warps)
        {
            if (!warp.PairedWarp)
            {
                Debug.LogWarning($"Warp {warp.name} does not have a paired warp!");
            }
        }
    }

    private void GenerateStandardInitialWalls()
    {
        PaintMode = PuzzleBuilderPaintMode.Wall;
        var targetWalls = Mathf.RoundToInt(Grid.Cells.Count * InitialWallAmount);

        if (targetWalls < 1)
        {
            return;
        }

        var placementTypeProbabilityTotal = InitialWallNormalness + InitialWallClustering + InitialWallNoodling;
        if (placementTypeProbabilityTotal == 0)
        {
            placementTypeProbabilityTotal = 1;
        }
        var cluster = InitialWallClustering / placementTypeProbabilityTotal;
        var noodle = cluster + InitialWallNoodling / placementTypeProbabilityTotal;

        for(int wallsGenerated = 0; wallsGenerated < targetWalls; wallsGenerated++)
        {
            var rand = Random.value;
            if(rand < cluster)
            {
                AddWallCluster();
            }
            else if(rand < noodle)
            {
                AddWallNoodle();
            }
            else
            {
                AddWallRandom();
            }
        }
    }

    private Dictionary<int, List<GridCell>> GeneratedSolutionPaths = new Dictionary<int, List<GridCell>>();
    private void GenerateStandardNodesPathsAndWaypoints()
    {
        PaintMode = PuzzleBuilderPaintMode.Node;
        List<List<GridCell>> contiguousGroupsOfCells;
        var colors = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            colors.Add(i);
        }
        var colorOrder = new int[colors.Count];
        for (int i = 0; i < colorOrder.Length; i++)
        {
            var randInd = Random.Range(0, colors.Count);
            colorOrder[i] = colors[randInd];
            colors.RemoveAt(randInd);
        }

        var waypointsLeftToGenerate = TargetWaypoints;
        var preferredNodeCells = new List<GridCell>();
        var placedColors = 0;
        var allPlaceholderCells = new List<GridCell>();

        for (int i = 0; i < TargetNodePairs; i++)
        {
            PaintNodeColor = colorOrder[i];
            contiguousGroupsOfCells = GetContiguousGroupsOfCells();

            List<GridCell> biggestGroup = null;
            int biggestGroupSize = 0;
            foreach (var group in contiguousGroupsOfCells)
            {
                if (group.Count > biggestGroupSize)
                {
                    biggestGroup = group;
                    biggestGroupSize = group.Count;
                }
            }

            if (biggestGroupSize < 4)
            {
                break; // Can't place any more node pairs
            }

            // Place nodes
            var possibleNodes = biggestGroup.Where(cell => !PlacedObjects.ContainsKey(cell)).ToList();
            var preferredNodes = possibleNodes.Intersect(preferredNodeCells).ToList();
            GridCell firstNode = null;
            if(preferredNodes.Any())
            {
                firstNode = preferredNodes[Random.Range(0, preferredNodes.Count)];
            }
            else
            {
                firstNode = possibleNodes[Random.Range(0, possibleNodes.Count)];
            }
            possibleNodes.Remove(firstNode);
            preferredNodes.Remove(firstNode);
            var distanceMap = new Dictionary<GridCell, int>();
            foreach(var cell in possibleNodes)
            {
                distanceMap.Add(cell, Grid.DistanceBetween(firstNode, cell));
            }
            List<GridCell> cellsFarEnoughAway = null;
            for(int distanceThreshold = PreferredDistanceBetweenNodes; distanceThreshold > 2; distanceThreshold--)
            {
                var matches = preferredNodes.Where(cell => distanceMap[cell] >= distanceThreshold);
                if(!matches.Any())
                {
                    matches = possibleNodes.Where(cell => distanceMap[cell] >= distanceThreshold);
                }
                if(matches.Any())
                {
                    cellsFarEnoughAway = matches.ToList();
                    break;
                }
            }
            if(cellsFarEnoughAway is null)
            {
                break; // Can't place any more node pairs
            }
            var secondNode = cellsFarEnoughAway[Random.Range(0, cellsFarEnoughAway.Count)];

            // "Micro" waywardness (waywardness initially choosing a segmentLength of 3
            //      then only continuing to modify the segment within that
            var placeholderCells = new List<GridCell>();
            var pathIncludingNodes = Grid.GetShortestPathPreferringWarps(firstNode, secondNode, Warps);
            pathIncludingNodes.Insert(0, firstNode);
            pathIncludingNodes.Add(secondNode);

            int microSegmentLength = 3;
            int maxOffsetVal = pathIncludingNodes.Count - microSegmentLength;
            int offsetMin = 0;
            int offsetMax = maxOffsetVal;

            // Randomly offset our starting point so it's not almost-always at one of the endpoints
            int offsetOffset = Random.Range(0, maxOffsetVal+1);
            offsetMin += offsetOffset;
            offsetMax += offsetOffset;

            for (int wayward = 0; wayward < MicroWaywardness; wayward++)
            {
                var adjustmentMade = false;
                for (int rawOffset = offsetMin; rawOffset <= offsetMax; rawOffset++)
                {
                    if (adjustmentMade)
                    {
                        break;
                    }
                    var offset = rawOffset % (maxOffsetVal + 1);

                    var start = pathIncludingNodes[offset];
                    var end = pathIncludingNodes[offset + microSegmentLength - 1];
                    for (int pathIndex = offset + 1; pathIndex < offset + microSegmentLength - 1; pathIndex++)
                    {
                        ColorCell(pathIncludingNodes[pathIndex], -1);
                        placeholderCells.Add(pathIncludingNodes[pathIndex]);
                        allPlaceholderCells.Add(pathIncludingNodes[pathIndex]);
                    }
                    Predicate<GridCell> obstructed = cell =>
                        cell.Color != null || cell.Neighbors.Any(n => {
                            var indexInPath = pathIncludingNodes.IndexOf(n);
                            return indexInPath >= 0 && (indexInPath < offset || indexInPath > offset + microSegmentLength - 1);
                        }) || pathIncludingNodes.Any(pathCell => cell.Neighbors.Intersect(pathCell.Neighbors.Where(pathNeighborCell => pathIncludingNodes.IndexOf(pathNeighborCell) < 0)).Count() > 1);
                    var newSubPath = Grid.GetShortestPathPreferringWarps(start, end, Warps, obstructed);
                    if (newSubPath != null && newSubPath.Count > 1)
                    {
                        pathIncludingNodes.RemoveRange(offset + 1, microSegmentLength - 2);
                        pathIncludingNodes.InsertRange(offset + 1, newSubPath);
                        offsetMin = offset + 1;
                        offsetMax = offsetMin + newSubPath.Count - microSegmentLength;
                        if(offsetMax < offsetMin)
                        {
                            offsetMin = offsetMax;
                            offsetMax = offsetMin + 2;
                        }
                        offsetMin = Mathf.Min(offsetMin, pathIncludingNodes.Count - microSegmentLength);
                        offsetMax = Mathf.Min(offsetMax, pathIncludingNodes.Count - microSegmentLength);
                        adjustmentMade = true;
                    }
                    else
                    {
                        for (int pathIndex = offset + 1; pathIndex < offset + microSegmentLength - 1; pathIndex++)
                        {
                            ColorCell(pathIncludingNodes[pathIndex], null);
                            placeholderCells.Remove(pathIncludingNodes[pathIndex]);
                            allPlaceholderCells.Remove(pathIncludingNodes[pathIndex]);
                        }
                    }
                }

                if (!adjustmentMade)
                {
                    break;
                }
            }

            foreach (var cell in placeholderCells)
            {
                ColorCell(cell, null);
            }

            var solutionPath = pathIncludingNodes.Except(new GridCell[]{ firstNode, secondNode }).ToList();
            foreach(var cell in solutionPath)
            {
                ColorCell(cell);
            }

            GeneratedSolutionPaths[colorOrder[i]] = solutionPath;

            PaintMode = PuzzleBuilderPaintMode.Node;
            Paint(firstNode);
            Paint(secondNode);

            preferredNodeCells = Grid.Cells.Where(cell => cell.IsDeadEnd()).ToList();

            placedColors++;
        }

        if(WallUnusedPlaceholders)
        {
            PaintMode = PuzzleBuilderPaintMode.Wall;
            foreach (var cell in allPlaceholderCells.Where(c => !PlacedObjects.ContainsKey(c)))
            {
                if (cell.Color is null)
                {
                    Paint(cell);
                }
            }

            for (int i = 0; i < placedColors; i++)
            {
                foreach (var cell in Grid.Cells.Where(c => !PlacedObjects.ContainsKey(c)))
                {
                    if (cell.Color is null && cell.Neighbors.All(n => n.Color == i || allPlaceholderCells.Contains(n)))
                    {
                        Paint(cell);
                    }
                }
            }
        }

        PaintMode = PuzzleBuilderPaintMode.Waypoint;
        for (int i = 0; i < placedColors && waypointsLeftToGenerate > 0; i++)
        {
            PaintNodeColor = colorOrder[i];
            var solution = GeneratedSolutionPaths[PaintNodeColor];

            bool isChokepoint(GridCell cell) =>
                cell.Neighbors.All(n => n.Color == PaintNodeColor || PlacedObjects.ContainsKey(n)) &&
                cell.Neighbors.Any(n => PlacedObjects.ContainsKey(n));

            var solutionChokepoints = solution.Where(cell => isChokepoint(cell) &&
                !PlacedObjects.ContainsKey(cell) &&
                !cell.Neighbors.Any(n => PlacedObjects.ContainsKey(n) && PlacedObjects[n] is PuzzleObjectNode neighborNode && neighborNode.Color == PaintNodeColor)).ToList();

            // If we have chokepoints, prefer one of those
            if (solutionChokepoints.Any())
            {
                Paint(solutionChokepoints[Random.Range(0, solutionChokepoints.Count - 1)]);
            }
            else
            {
                var solutionCellsToAcceptWaypoint = solution.Where(cell => !PlacedObjects.ContainsKey(cell)).ToList();
                if(solutionCellsToAcceptWaypoint.Count < 1)
                {
                    Debug.LogError($"Could not add waypoint to solution for color {PaintNodeColor} because it is already occupied by other objects");
                    continue;
                }
                // Paint a waypoint to a part of the solution between 40% and 60% of the way through
                var min = Mathf.CeilToInt((solutionCellsToAcceptWaypoint.Count + 1) * 0.4f);
                var max = Mathf.FloorToInt((solutionCellsToAcceptWaypoint.Count + 1) * 0.6f);
                var rand = Random.Range(min, max + 1);

                Paint(solutionCellsToAcceptWaypoint[rand - 1]);
            }


            waypointsLeftToGenerate--;
        }
    }

    private void GenerateStandardAdditionalWalls()
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
    #endregion

    #region Common
    private void RemoveUnusedWarps()
    {
        var warpsToRemove = new List<PuzzleObjectWarp>();
        foreach (var kvp in GeneratedSolutionPaths)
        {
            var path = kvp.Value;

            for (int i = 0; i < path.Count; i++)
            {
                if (PlacedObjects.ContainsKey(path[i]) && PlacedObjects[path[i]] is PuzzleObjectWarp warp)
                {
                    if (i > 0 && PlacedObjects.ContainsKey(path[i - 1]) && PlacedObjects[path[i - 1]] is PuzzleObjectWarp otherWarpPrev && warp.PairedWarp == otherWarpPrev)
                    {
                        continue;
                    }
                    if (i < path.Count - 1 && PlacedObjects.ContainsKey(path[i + 1]) && PlacedObjects[path[i + 1]] is PuzzleObjectWarp otherWarpNext && warp.PairedWarp == otherWarpNext)
                    {
                        continue;
                    }
                    warpsToRemove.Add(warp);
                    warpsToRemove.Add(warp.PairedWarp);
                }
            }
        }

        foreach(var warp in Warps)
        {
            if(warp.Cell.Color is null || warp.Cell.Color < 0)
            {
                warpsToRemove.Add(warp);
                warpsToRemove.Add(warp.PairedWarp);
            }
        }

        warpsToRemove = warpsToRemove.Where(w => w != null).Distinct().ToList();

        if (warpsToRemove.Count > 0)
        {
            Debug.Log($"Removing {warpsToRemove.Count} unused warps after generating solution paths");
        }
        foreach (var warp in warpsToRemove)
        {
            EraseObject(warp.Cell, isManual: false);
        }
    }
    #endregion

    #region Generation Util
    private void AddWallRandom()
    {
        PaintMode = PuzzleBuilderPaintMode.Wall;
        var availableCells = GetOpenCells(Grid.Cells).ToList();
        if(!availableCells.Any())
        {
            return;
        }
        Paint(availableCells[Random.Range(0, availableCells.Count)]);
    }
    private void AddWallCluster()
    {
        PaintMode = PuzzleBuilderPaintMode.Wall;
        var availableCellGroups = GetOpenCells(Grid.Cells)
            .GroupBy(c => c.Neighbors.Count(n => PlacedObjects.ContainsKey(n) && PlacedObjects[n] is PuzzleObjectWall)).ToList();
        if(!availableCellGroups.Any())
        {
            return;
        }
        IGrouping<int, GridCell> bestGroup = null;
        var bestGroupNeighborCount = -1;
        foreach(var group in availableCellGroups)
        {
            if(group.Key > bestGroupNeighborCount)
            {
                bestGroupNeighborCount = group.Key;
                bestGroup = group;
            }
        }
        var cellsToChooseFrom = bestGroup.ToList();
        Paint(cellsToChooseFrom[Random.Range(0, cellsToChooseFrom.Count)]);
    }
    private void AddWallNoodle()
    {
        PaintMode = PuzzleBuilderPaintMode.Wall;
        var availableCellGroups = GetOpenCells(Grid.Cells)
            .GroupBy(c => c.Neighbors.Count(n => PlacedObjects.ContainsKey(n) && PlacedObjects[n] is PuzzleObjectWall)).ToList();
        if (!availableCellGroups.Any())
        {
            return;
        }
        IGrouping<int, GridCell> bestGroup = null;
        foreach (var group in availableCellGroups)
        {
            if(group.Key == 1)
            {
                bestGroup = group;
                break;
            }
            if(group.Key == 0)
            {
                bestGroup = group;

            }
            else if(bestGroup is null || bestGroup.Key > 0 && group.Key < bestGroup.Key)
            {
                bestGroup = group;
            }
        }
        var cellsToChooseFrom = bestGroup.ToList();
        Paint(cellsToChooseFrom[Random.Range(0, cellsToChooseFrom.Count)]);
    }

    private List<List<GridCell>> GetContiguousGroupsOfCells()
    {
        var groups = new List<List<GridCell>>();
        var ungroupedCells = GetOpenCells(Grid.Cells).ToList();
        while (ungroupedCells.Any())
        {
            var group = new List<GridCell>();
            groups.Add(group);
            group.AddRange(Grid.GetContiguousCells(ungroupedCells.First()));
            foreach (var cell in group)
            {
                ungroupedCells.Remove(cell);
            }
        }
        return groups;
    }

    private IEnumerable<GridCell> GetOpenCells(IEnumerable<GridCell> toConsider) => toConsider.Where(c => c.Color is null && !PlacedObjects.ContainsKey(c));
	#endregion

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
            PlacedObjects.Remove(warp.Cell);
            warp.Unpair(issueWarning: false);
            Destroy(warp.gameObject);
        }
        Warps.Clear();
        LastPlacedWarp = null;
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
    public void Save()
    {
        if(string.IsNullOrWhiteSpace(Id))
        {
            Debug.LogError("Cannot save puzzle with empty name");
            return;
        }

        var newPuzzleConfig = ScriptableObject.CreateInstance<PuzzleConfig>();

        // Metadata
        newPuzzleConfig.Pack = Pack;
        newPuzzleConfig.Id = Id;

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

        if (!AssetDatabase.IsValidFolder($"Assets/Resources/Puzzles/{Pack}"))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Puzzles", Pack);
        }
        AssetDatabase.CreateAsset(newPuzzleConfig, $"Assets/Resources/Puzzles/{Pack}/{Id}.asset");
    }

    public void Load(PuzzleConfig cfg)
    {
        Clear();

        // Metadata
        Pack = cfg.Pack;
        Id = cfg.Id;

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

public enum PuzzleGenerationMode
{
    Standard,
    Hemispheres
}
#endif