using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PuzzleBuilder : MonoBehaviour
{
    [Header("Required References")]
    public PuzzleGrid Grid;
    public Camera Camera;
    public CameraMotorUpright CameraMotor;

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

    [Header("Saving")]
    public string PuzzleName = "Puzzle";
    public bool SavePuzzle = false;

    [Header("Loading")]
    public GriddedPuzzleConfig ConfigToLoad;
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

    private void OnValidate()
    {
        if(SavePuzzle)
        {
            SavePuzzle = false;
            Save(PuzzleName);
        }
        if(LoadPuzzle)
        {
            LoadPuzzle = false;
            Load(ConfigToLoad);
        }
        if(DoGridRebuild)
        {
            DoGridRebuild = false;
            Clear();
            RebuildGrid();
        }
    }

    void Update()
    {
        if(Input.GetMouseButtonUp(0) && CameraMotor.PanAmountThisDrag < 1f) // Left click, no dragging
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
    }

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
    private void ColorCell(GridCell cell, int color)
    {
        var col = color == -1 ? Gray : ColorMapController.Instance.ApplyActiveColorMap(color);
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

	#region Reset Functions
	public void Clear()
    {
        ClearNodes();
        //ClearPaths();
        ClearObstacles();
    }

    public void ClearNodes()
    {
        foreach(var node in Nodes)
        {
            Destroy(node.gameObject);
        }
        Nodes.Clear();
    }

    //public void ClearPaths()
    //{
    //    foreach(var kvp in Puzzle.Paths)
    //    {
    //        Destroy(kvp.Item2.gameObject);
    //    }
    //    Puzzle.Paths.Clear();
    //}

    public void ClearObstacles()
    {
        foreach (var wall in Walls)
        {
            Destroy(wall);
        }
        Walls.Clear();
        foreach (var rock in Rocks)
        {
            Destroy(rock);
        }
        Rocks.Clear();
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
        var newPuzzleConfig = ScriptableObject.CreateInstance<GriddedPuzzleConfig>();

        newPuzzleConfig.GridCellsPerRow = GridCellsPerRow;

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

        newPuzzleConfig.WallPositions = new Vector2Int[Walls.Count];
        for (i = 0; i < Walls.Count; i++)
        {
            newPuzzleConfig.WallPositions[i] = new Vector2Int(Walls[i].Cell.Row, Walls[i].Cell.Cell);
        }

        newPuzzleConfig.RockPositions = new Vector2Int[Rocks.Count];
        for(i = 0; i < Rocks.Count; i++)
        {
            newPuzzleConfig.RockPositions[i] = new Vector2Int(Rocks[i].Cell.Row, Rocks[i].Cell.Cell);
        }

        AssetDatabase.CreateAsset(newPuzzleConfig, $"Assets/Puzzles/{puzzleName}.asset");
    }

    public void Load(GriddedPuzzleConfig cfg)
    {
        Clear();
        GridCellsPerRow = cfg.GridCellsPerRow;
        RebuildGrid();
        PaintMode = PuzzleBuilderPaintMode.Node;
        for(int i = 0; i < cfg.NodePositions.Length; i++)
        {
            var row = cfg.NodePositions[i].x;
            var rowCell = cfg.NodePositions[i].y;
            PaintNodeColor = cfg.NodeColors.Length > i ? cfg.NodeColors[i] : Mathf.FloorToInt(i / 2f);
            Paint(Grid.CellsByRow[row][rowCell]);
        }
        PaintMode = PuzzleBuilderPaintMode.Wall;
        for (int i = 0; i < cfg.WallPositions.Length; i++)
        {
            var row = cfg.WallPositions[i].x;
            var rowCell = cfg.WallPositions[i].y;
            Paint(Grid.CellsByRow[row][rowCell]);
        }
        PaintMode = PuzzleBuilderPaintMode.Rock;
        for(int i = 0; i < cfg.RockPositions.Length; i++)
        {
            var row = cfg.RockPositions[i].x;
            var rowCell = cfg.RockPositions[i].y;
            Paint(Grid.CellsByRow[row][rowCell]);
        }
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