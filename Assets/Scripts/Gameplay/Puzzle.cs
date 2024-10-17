using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Puzzle : MonoBehaviour
{
    public static Puzzle Current { get; set; }

    [Header("Prefabs")]
    public GameObject NodePrefab;
    public GameObject RockPrefab;
    public GameObject WallPrefab;

    [Header("Required References")]
    public PuzzleGrid Grid;
    public Camera PuzzleViewCamera;
    public CameraMotor PuzzleCameraMotor;

    [Header("Settings")]
    public float PanInsteadOfSelectionThreshold = 1f;

    [Header("State")]
    public List<Node> Nodes;
    public Dictionary<int, List<Node>> NodesByColor;
    public Node ActiveNode;
    public List<(int, LineRenderer)> Paths;
    public List<GameObject> Rocks;
    public List<Wall> Walls;

    private float PathConnectToNodeDistance = 0.1f;
    private float PathCollisionDistance = 0.07f;
    private float NodeCollisionDistance = 0.15f;

    public bool Panning { get; private set; }

    [Header("Debug")]
    public GriddedPuzzleConfig DEBUG_PUZZLE_CONFIG;
    public bool GridVisible = false;

    // Start is called before the first frame update
    void Start()
    {
        Grid.Initialize(DEBUG_PUZZLE_CONFIG.GridCellsPerRow, gridVisible: GridVisible);

        PathCollisionDistance = Grid.ClosestDistanceBetweenNeighbors * 0.3f;
        NodeCollisionDistance = Grid.ClosestDistanceBetweenNeighbors * 0.5f;

        Debug.Log($"PathCollisionDistance: {PathCollisionDistance}");
        Debug.Log($"NodeCollisionDistance: {NodeCollisionDistance}");

        Current = this;
        if(DEBUG_PUZZLE_CONFIG != null)
        {
            SetupPuzzle(DEBUG_PUZZLE_CONFIG);
        }
        ColorMapController.Instance.ColorMapChanged += OnColorMapChanged;
    }

    private void OnDestroy()
    {
        ColorMapController.Instance.ColorMapChanged -= OnColorMapChanged;
    }

    private void OnColorMapChanged()
    {
        foreach(var node in Nodes)
        {
            node.SetColor(node.Color);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Panning)
        {
            if(ActiveNode)
            {
                var point = PuzzleViewCamera.transform.position.normalized;
                ActiveNode.Draw(point);
                SmoothEndOfLine(ActiveNode.Path);
                DejitterEndOfLine(ActiveNode.Path);

                if(Vector3.Distance(ActiveNode.PairedNode.transform.position, point) < PathConnectToNodeDistance)
                {
                    ActiveNode.Draw(ActiveNode.PairedNode.transform.position);
                    SmoothEndOfLine(ActiveNode.Path);
                    DejitterEndOfLine(ActiveNode.Path);
                    if (ActiveNode.PairedNode.Path)
                    {
                        Destroy(ActiveNode.PairedNode.Path.gameObject);
                    }
                    Paths.RemoveAll(p => !p.Item2);
                    ActiveNode.PairedNode.Path = ActiveNode.Path;
                    SetConnected(ActiveNode, ActiveNode.PairedNode);
                }
            }
        }
    }

    private void OnMouseDrag()
    {
        Panning = true;
    }
    private void OnMouseExit()
    {
        Panning = false;
    }
    private void OnMouseUp()
    {
        Panning = false;
    }
    public void NodeOnMouseUp(Node n)
    {
        if (!PuzzleCameraMotor.Panning && PuzzleCameraMotor.PanAmountThisDrag < PanInsteadOfSelectionThreshold)
        {
            SetActiveNode(n);
        }
    }

    public bool IsComplete() => Nodes.TrueForAll(n => n.Connected);

    private const float PATH_SIZE_RELATIVE_TO_NODE_SIZE = 0.43f;
    public void SetActiveNode(Node n)
    {
        if(!n)
        {
            return;
        }

        PuzzleCameraMotor.SnapToNode(n);

        if(ActiveNode)
        {
            ActiveNode.Deactivate();
        }
        ActiveNode = n;
        n.Activate();
        Paths.RemoveAll(p => !p.Item2);
        Paths.Add((n.Color, n.Path));
        n.Path.startWidth = n.transform.localScale.x * PATH_SIZE_RELATIVE_TO_NODE_SIZE;
        n.Path.endWidth = n.transform.localScale.x * PATH_SIZE_RELATIVE_TO_NODE_SIZE;
    }

    public void SetConnected(Node a, Node b)
    {
        a.Connected = true;
        b.Connected = true;
        a.Deactivate();
        b.Deactivate();
        ActiveNode = null;
    }

	#region Setup
	public void SetupPuzzle(GriddedPuzzleConfig cfg)
    {
        SetupNodes(cfg);
        SetupObstacles(cfg);
    }

    private const float NODE_VISUAL_SCALE_FACTOR = 2.24f;
    private void SetupNodes(GriddedPuzzleConfig cfg)
    {
        ActiveNode = null;
        var children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        Nodes = new List<Node>();
        NodesByColor = new Dictionary<int, List<Node>>();
        Paths = new List<(int, LineRenderer)>();

        float nodeVisualScale = NodeCollisionDistance * NODE_VISUAL_SCALE_FACTOR;

        Func<int, int> colorGetter = (i) => Mathf.FloorToInt(i / 2f);
        if (cfg.NodeColors != null && cfg.NodeColors.Length >= cfg.NodePositions.Length)
        {
            colorGetter = (i) => cfg.NodeColors[i];
        }

        for (int i = 0; i < cfg.NodePositions.Length; i++)
        {
            var newNodeGO = Instantiate(NodePrefab);
            newNodeGO.transform.parent = transform;
            var newNode = newNodeGO.GetComponent<Node>();
            var colorIndex = colorGetter(i);
            newNodeGO.name = $"{ColorMapController.Instance.ColorName(colorIndex)} Node";
            newNode.SetColor(colorIndex);
            newNode.Puzzle = this;

            var nodePosition = cfg.NodePositions[i];

            var cell = Grid.CellsByRow[nodePosition.x][nodePosition.y];
            newNode.GridCell = cell;
            cell.Color = colorIndex;
            newNode.transform.position = cell.transform.position;

            newNode.transform.localScale = new Vector3(nodeVisualScale, nodeVisualScale, nodeVisualScale);

            Nodes.Add(newNode);
            if (NodesByColor.ContainsKey(colorIndex))
            {
                NodesByColor[colorIndex].Add(newNode);
            }
            else
            {
                NodesByColor.Add(colorIndex, new List<Node>() { newNode });
            }
        }

        foreach (var pair in Nodes.GroupBy(n => n.Color))
        {
            if (pair.Count() != 2)
            {
                Debug.LogError($"There should be 2 nodes of color {pair.Key}, but there are {pair.Count()}!");
            }
            var first = pair.ElementAt(0);
            var second = pair.ElementAt(1);
            first.SetPairedNode(second);
            second.SetPairedNode(first);
        }
    }

    private void SetupObstacles(GriddedPuzzleConfig cfg)
    {
        SetupRocks(cfg);
        SetupWalls(cfg);
    }

    private void SetupRocks(GriddedPuzzleConfig cfg)
    {
        for (int i = 0; i < cfg.RockPositions.Length; i++)
        {
            var row = cfg.RockPositions[i].x;
            var rowCell = cfg.RockPositions[i].y;

            var newRockGO = Instantiate(RockPrefab);
            newRockGO.transform.parent = transform;
            newRockGO.name = $"Rock r{row}c{rowCell}";

            var rockPosition = cfg.RockPositions[i];

            var cell = Grid.CellsByRow[rockPosition.x][rockPosition.y];
            cell.Color = -1;
            newRockGO.transform.position = cell.transform.position;
            newRockGO.transform.LookAt(transform);

            Rocks.Add(newRockGO);
        }
    }

    private void SetupWalls(GriddedPuzzleConfig cfg)
    {
        for (int i = 0; i < cfg.WallPositions.Length; i++)
        {
            var row = cfg.WallPositions[i].x;
            var rowCell = cfg.WallPositions[i].y;

            var newWallGO = Instantiate(WallPrefab);
            newWallGO.transform.parent = transform;
            newWallGO.name = $"Wall r{row}c{rowCell}";

            var wallPosition = cfg.WallPositions[i];

            var cell = Grid.CellsByRow[wallPosition.x][wallPosition.y];
            cell.Color = -1;

            var newWall = newWallGO.GetComponent<Wall>();
            newWall.SetGridCell(cell);

            Walls.Add(newWall);
        }
    }
	#endregion

    private const float ROCK_COLLISION_DISTANCE = 0.18f;
    public bool IsCameraPositionValid()
    {
        if(!ActiveNode)
        {
            return true;
        }
        var position = (PuzzleViewCamera.transform.position - transform.position).normalized;
        return IsPositionFree(position, ActiveNode.Color);
    }

    public bool IsPositionFree(Vector3 position, int? excludeColor = null)
    {
        var nodePathCollisionDistance = NodeCollisionDistance + PathCollisionDistance;
        foreach (var node in Nodes)
        {
            if (node.Color != excludeColor)
            {
                if (Vector3.Distance(position, node.transform.position) < nodePathCollisionDistance)
                {
                    return false;
                }
            }
        }
        var pathPathCollisionDistance = PathCollisionDistance * 2;
        foreach (var pair in Paths)
        {
            if (pair.Item1 != excludeColor)
            {
                var path = pair.Item2;
                for (int i = 0; i < path.positionCount; i++)
                {
                    if (Vector3.Distance(position, path.GetPosition(i)) < pathPathCollisionDistance)
                    {
                        return false;
                    }
                }
            }
        }
        var rockPathCollisionDistance = ROCK_COLLISION_DISTANCE + PathCollisionDistance;
        foreach (var rock in Rocks)
        {
            if (Vector3.Distance(position, rock.transform.position) < rockPathCollisionDistance)
            {
                return false;
            }
        }
        var wallPathCollisionPadding = Mathf.Tan(PathCollisionDistance) * Mathf.Rad2Deg;
        var pointPolar = position.ToPolar();
        foreach (var wall in Walls)
        {
            // Latitude "minimum" is the top, so it's actually the max
            if(pointPolar.Latitude < wall.GridCell.LatitudeMin + wallPathCollisionPadding && pointPolar.Latitude > wall.GridCell.LatitudeMax - wallPathCollisionPadding &&
                pointPolar.Longitude > wall.GridCell.LongitudeMin - wallPathCollisionPadding && pointPolar.Longitude < wall.GridCell.LongitudeMax + wallPathCollisionPadding)
            {
                return false;
            }
        }
        return true;
    }

    public void AutoConnectColor(int color)
    {
        var nodes = NodesByColor[color];
        if(nodes[0].Connected)
        {
            return;
        }

        nodes[0].AutoConnectToPairedNode();
    }

    [Header("Line Smoothing")]
    public float LineSmoothDistanceWeight = 1f;
    public float LineSmoothAngleWeight = 1f;
    public float MaxSharpness = 10f;
    public bool SmoothLineSegment(Vector3 p1, Vector3 p2, Vector3 p3, out Vector3 resultingP2)
    {
        var p1p2 = p2 - p1;
        var p2p3 = p3 - p2;
        var p1p2Len = Vector3.Magnitude(p1p2);
        var p2p3Len = Vector3.Magnitude(p2p3);

        var distance = p1p2Len + p2p3Len;
        var angle = Vector3.Angle(p2 - p1, p3 - p2);

        var sharpness = (angle * LineSmoothAngleWeight) / (distance * LineSmoothDistanceWeight);

        if(sharpness <= MaxSharpness)
        {
            resultingP2 = p2;
            return false;
        }

        var maxAngle = MaxSharpness * distance * LineSmoothDistanceWeight / LineSmoothAngleWeight;
        var angleOutside = angle - maxAngle;

        var straightP2 = Vector3.Lerp(p1, p3, p1p2Len / (distance));
        var p1p2Straight = straightP2 - p1;
        var p1p2Alt = Vector3.RotateTowards(p1p2, p1p2Straight, angleOutside, float.MaxValue);
        resultingP2 = p1 + p1p2Alt;

        return true;
    }

    public void SmoothEndOfLine(LineRenderer renderer)
    {
        var positionCount = renderer.positionCount;
        if(positionCount < 3)
        {
            return;
        }

        var p1 = renderer.GetPosition(positionCount - 3);
        var p2 = renderer.GetPosition(positionCount - 2);
        var p3 = renderer.GetPosition(positionCount - 1);

        if(SmoothLineSegment(p1, p2, p3, out Vector3 resultingP2))
        {
            renderer.SetPosition(positionCount - 2, resultingP2);
        }
    }

    public bool DejitterLineSegment(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out Vector3 resultingP2, out Vector3 resultingP3)
    {
        var p1p2 = p2 - p1;
        var p2p3 = p3 - p2;
        var p3p4 = p4 - p3;
        var p1p4 = p4 - p1;

        var p1p2Flat = Vector3.ProjectOnPlane(p1p2, p1p4);
        var p2p3Flat = Vector3.ProjectOnPlane(p2p3, p1p4);
        var p3p4Flat = Vector3.ProjectOnPlane(p3p4, p1p4);

        var isZigZag = Vector3.Dot(p1p2Flat, p2p3Flat) < 0 &&
            Vector3.Dot(p3p4Flat, p2p3Flat) < 0;

        //var p1p2Cross = Vector3.Cross(p1p2, p1p4);
        //var p2p3Cross = Vector3.Cross(p2p3, p1p4);
        //var p3p4Cross = Vector3.Cross(p3p4, p1p4);

        //var isZigZag = Vector3.Dot(p1p2Cross, p2p3Cross) < 0 &&
        //    Vector3.Dot(p3p4Cross, p2p3Cross) < 0;

        if (!isZigZag)
        {
            resultingP2 = p2;
            resultingP3 = p3;
            return false;
        }

        var p1p3 = p3 - p1;

        var p1p2Alt = Vector3.Project(p1p2, p1p4);
        var p1p3Alt = Vector3.Project(p1p3, p1p4);
        resultingP2 = p1 + p1p2Alt;
        resultingP3 = p1 + p1p3Alt;

        return true;
    }

    public void DejitterEndOfLine(LineRenderer renderer)
    {
        var positionCount = renderer.positionCount;
        if (positionCount < 4)
        {
            return;
        }

        var p1 = renderer.GetPosition(positionCount - 4);
        var p2 = renderer.GetPosition(positionCount - 3);
        var p3 = renderer.GetPosition(positionCount - 2);
        var p4 = renderer.GetPosition(positionCount - 1);

        if (DejitterLineSegment(p1, p2, p3, p4, out Vector3 resultingP2, out Vector3 resultingP3))
        {
            renderer.SetPosition(positionCount - 3, resultingP2);
            renderer.SetPosition(positionCount - 2, resultingP3);
            Debug.Log("Dejittered!");
        }
    }
}
