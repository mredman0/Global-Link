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
    public CameraController CameraController;

    [Header("Prefabs")]
    public GameObject NodePrefab;
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
    public List<PuzzleObjectWall> Walls = new List<PuzzleObjectWall>();
    public List<PuzzleObjectRock> Rocks = new List<PuzzleObjectRock>();

    [Header("Camera Settings")]
    public Quaternion CameraArmStart;
    public bool SnapCameraArmStartToCurrent;
    public bool SnapCameraArmToStart;

    public float CameraDistance;
    public float CameraFoV;
    public bool SnapToCameraSettings;

    [Header("Generator")]
    public string GeneratorSeed = "";
    [Range(0, 1)]
    public float WallAmount = 0f;
    [Range(0, 1)]
    [Tooltip("0 means each wall is selected individually,\n0.25 means walls will be grouped into ~4 clusters,\n0.5 means ~2 clusters,\n1 means a single cluster")]
    public float WallClustering = 0f;
    [Range(1, 6)]
    public int MaxNodePairs = 6;
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
    }

    void Update()
    {
        if(Input.GetMouseButtonUp(0) && CameraController.PanAmountThisDrag < 1f) // Left click, no dragging
        {
            var ray = Camera.ScreenPointToRay(Input.mousePosition);
            var anyHit = Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, MAIN_SPHERE_LAYER_MASK);
            if(anyHit)
            {
                var hitPoint = hitInfo.point;
                GridCell closestCell = null;
                float closestCellDistance = float.MaxValue;
                foreach(var cell in Grid.Cells)
                {
                    var dist = Vector3.Distance(cell.transform.position, hitPoint);
                    if (dist < closestCellDistance)
                    {
                        closestCell = cell;
                        closestCellDistance = dist;
                    }
                }
                if(closestCell is null)
                {
                    Debug.LogWarning("Could not find cell to paint to");
                    return;
                }
                Paint(closestCell);
            }
        }

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

        GenerateWalls();
        GenerateNodesAndPaths();
    }

    private void SetRandomSeed()
    {
        int seed = string.IsNullOrEmpty(GeneratorSeed) ? (int)DateTime.Now.Ticks : GeneratorSeed.GetHashCode();
        if (int.TryParse(GeneratorSeed, out int seedNumber))
        {
            seed = seedNumber;
        }
        Random.InitState(seed);
    }
	
    private void GenerateWalls()
    {
        PaintMode = PuzzleBuilderPaintMode.Wall;
        var targetWalls = Mathf.RoundToInt(Grid.Cells.Count * WallAmount);

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

    private void GenerateNodesAndPaths()
    {
        PaintMode = PuzzleBuilderPaintMode.Node;
        List<List<GridCell>> contiguousGroupsOfCells;
        for(int i = 0; i < MaxNodePairs; i++)
        {
            PaintNodeColor = i;
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

            if(biggestGroupSize < 2)
            {
                break; // Can't place any more node pairs
            }

            var firstNode = biggestGroup[Random.Range(0, biggestGroup.Count)];
            biggestGroup.Remove(firstNode);
            var secondNode = biggestGroup[Random.Range(0, biggestGroup.Count)];

            foreach(var cell in Grid.GetShortestPath(firstNode, secondNode, cell => cell.Color != null))
            {
                ColorCell(cell);
            }
            Paint(firstNode);
            Paint(secondNode);
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
            group.AddRange(Grid.GetContiguousCells(ungroupedCells.First(), cell => cell.Color != null));
            foreach(var cell in group)
            {
                ungroupedCells.Remove(cell);
            }
        }
        return groups;
    }

    private IEnumerable<GridCell> GetOpenCells(IEnumerable<GridCell> toConsider) => toConsider.Where(c => c.Color is null);
    #endregion

	#region Reset Functions
	public void Clear()
    {
        ClearNodes();
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
        var newPuzzleConfig = ScriptableObject.CreateInstance<PuzzleConfig>();

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
            var steps = new List<GridCell>();
            var nodes = Nodes.Where(n => n.Color == color).ToList();
            if(nodes.Count == 2)
            {
                var start = nodes[0].Cell;
                var end = nodes[1].Cell;
                var current = start;
                while(current != end)
                {
                    var next = current.Neighbors.FirstOrDefault(n => n.Color == color && n != start && !steps.Contains(n));
                    if(!next)
                    {
                        Debug.LogWarning($"Could not follow solution path for color {color}");
                        break;
                    }
                    if(next != end)
                    {
                        steps.Add(next);
                    }
                    current = next;
                }
            }
            else if(nodes.Any())
            {
                Debug.LogWarning($"Color {color} does not have exactly 2 nodes, not storing solution.");
            }
            solutions.AddRange(steps);
            solutionLengths[color] = steps.Count;
        }
        newPuzzleConfig.SolutionLengths = solutionLengths;
        newPuzzleConfig.Solutions = solutions.Select(c => new Vector2Int(c.Row, c.Cell)).ToArray();

        // Camera Settings
        newPuzzleConfig.CameraArmStart = CameraArmStart;
        newPuzzleConfig.CameraDistance = CameraDistance;
        newPuzzleConfig.CameraFoV = CameraFoV;

        AssetDatabase.CreateAsset(newPuzzleConfig, $"Assets/Puzzles/{puzzleName}.asset");
    }

    public void Load(PuzzleConfig cfg)
    {
        Clear();
        // Grid
        GridCellsPerRow = cfg.GridCellsPerRow;
        RebuildGrid();
        // Nodes
        PaintMode = PuzzleBuilderPaintMode.Node;
        for(int i = 0; i < cfg.NodePositions.Length; i++)
        {
            var row = cfg.NodePositions[i].x;
            var rowCell = cfg.NodePositions[i].y;
            PaintNodeColor = cfg.NodeColors.Length > i ? cfg.NodeColors[i] : Mathf.FloorToInt(i / 2f);
            Paint(Grid.CellsByRow[row][rowCell]);
        }
        // Walls
        PaintMode = PuzzleBuilderPaintMode.Wall;
        for (int i = 0; i < cfg.WallPositions.Length; i++)
        {
            var row = cfg.WallPositions[i].x;
            var rowCell = cfg.WallPositions[i].y;
            Paint(Grid.CellsByRow[row][rowCell]);
        }
        // Rocks
        PaintMode = PuzzleBuilderPaintMode.Rock;
        for(int i = 0; i < cfg.RockPositions.Length; i++)
        {
            var row = cfg.RockPositions[i].x;
            var rowCell = cfg.RockPositions[i].y;
            Paint(Grid.CellsByRow[row][rowCell]);
        }
        // Solutions
        PaintMode = PuzzleBuilderPaintMode.Node;
        var currentStep = 0;
        for(int i = 0; i < 6; i++)
        {
            var lengthOfSolution = cfg.SolutionLengths[i];
            var end = currentStep + lengthOfSolution;
            PaintNodeColor = i;
            for(; currentStep < end; currentStep++)
            {
                var row = cfg.Solutions[currentStep].x;
                var rowCell = cfg.Solutions[currentStep].y;
                ColorCell(Grid.CellsByRow[row][rowCell]);
            }
        }

        // Camera Settings
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
    Wall,
    Rock,
}