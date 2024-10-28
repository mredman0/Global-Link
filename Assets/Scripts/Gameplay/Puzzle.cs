using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Puzzle : MonoBehaviour
{
    public static Puzzle Current { get; set; }

    public event Action PuzzleInitialized;
    public event Action<Node> NodeSelected;
    public event Action NodeDeselected;
    public event Action<Node, Node> NodesConnected;
    public event Action<Node, Node> NodesDisconnected;
    public event Action<Waypoint> WaypointReached;
    public event Action<Waypoint> WaypointUnreached;
    public event Action<Warp, Warp> WarpTaken;
    public event Action<Warp, Warp> WarpUntaken;
    public event Action PuzzleCompleted;

    [Header("Prefabs")]
    public GameObject NodePrefab;
    public GameObject WaypointPrefab;
    public GameObject WarpPrefab;
    public GameObject RockPrefab;
    public GameObject WallPrefab;

    [Header("Effects")]
    public List<GameObject> NodesConnectedEffects;
    public List<GameObject> WaypointColoredEffects;
    public GameObject PuzzleCompleteEffect;

    [Header("Required References")]
    public PuzzleGrid Grid;
    public Camera PuzzleViewCamera;
    public CameraController CameraController;
    public MeshRenderer MainSphere;

    [Header("Settings")]
    public float PanInsteadOfSelectionThreshold = 1f;
    public List<RectTransform> IgnoreInputRegions = new List<RectTransform>();

    [Header("State")]
    public bool Initialized = false;
    public Node ActiveNode;
    public List<Node> Nodes;
    public Dictionary<int, List<Node>> NodesByColor = new Dictionary<int, List<Node>>();
    public List<Waypoint> Waypoints;
    public Dictionary<GridCell, Waypoint> WaypointsByGridCell = new Dictionary<GridCell, Waypoint>();
    public List<Warp> Warps;
    public Dictionary<GridCell, Warp> WarpsByGridCell = new Dictionary<GridCell, Warp>();
    public Dictionary<int, LineRenderer> Paths = new Dictionary<int, LineRenderer>();
    public List<GameObject> Rocks;
    public List<Wall> Walls;

    public int InputLocks = 0;
    public bool Completed;

    private float PathConnectToNodeDistance = 0.1f;
    private float PathCollisionDistance = 0.07f;
    private float NodeCollisionDistance = 0.15f;

    private PuzzleConfig PuzzleConfig;

    [Header("Debug")]
    public bool GridVisible = false;

    private void Awake()
    {
        Current = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        InitializePuzzle();

        InputManager.Instance.Tap += OnTap;
        ColorMapController.Instance.ColorMapChanged += OnColorMapChanged;

        if (TutorialInstructionsProvider.Instance)
        {
            var tutorialInstructions = TutorialInstructionsProvider.Instance.GetTutorialInstructionsPrefab(PuzzleConfig.ID);
            if(tutorialInstructions)
            {
                Instantiate(tutorialInstructions);
            }
        }
    }

    private void OnDestroy()
    {
        ColorMapController.Instance.ColorMapChanged -= OnColorMapChanged;
        InputManager.Instance.Tap -= OnTap;
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
        if(ActiveNode)
        {
            HandleDrawingForActiveNode();
        }
    }

    private Vector3 PreviousDrawPoint;
    private void HandleDrawingForActiveNode()
    {
        var point = PuzzleViewCamera.transform.position.normalized;
        if (point == PreviousDrawPoint)
        {
            return;
        }
        PreviousDrawPoint = point;

        Draw(ActiveNode, point);
        SmoothEndOfLine(ActiveNode.Path, ActiveNode.Color);
        DejitterEndOfLine(ActiveNode.Path, ActiveNode.Color);

        if (Vector3.Distance(ActiveNode.PairedNode.transform.position, point) < PathConnectToNodeDistance)
        {
            Draw(ActiveNode, ActiveNode.PairedNode.transform.position);
            SmoothEndOfLine(ActiveNode.Path, ActiveNode.Color);
            DejitterEndOfLine(ActiveNode.Path, ActiveNode.Color);
            //Paths.RemoveAll(p => !p.Item2);
            ActiveNode.PairedNode.Path = ActiveNode.Path;
            SetConnected(ActiveNode, ActiveNode.PairedNode);
        }
    }

    private const float MINIMUM_DRAW_STEP = 0.01f;
    private void Draw(Node node, Vector3 point)
    {
        var path = node.Path;
        var loopMergeDistance = path.endWidth * 0.8f;
        int mergeLoop;
        int mergeIgnoreMostRecent = Mathf.FloorToInt(loopMergeDistance / MINIMUM_DRAW_STEP);
        for (mergeLoop = 0; mergeLoop < path.positionCount - mergeIgnoreMostRecent; mergeLoop++)
        {
            if ((point - path.GetPosition(mergeLoop)).magnitude < loopMergeDistance)
            {
                for(int i = mergeLoop + 1; i < path.positionCount; i++)
                {
                    NotifyWaypointsOfLinePointRemoved(path.GetPosition(i));
                    NotifyWarpsOfLinePointRemoved(path.GetPosition(i));
                }
                path.positionCount = mergeLoop + 1;
                break;
            }
        }

        if ((point - path.GetPosition(path.positionCount - 1)).magnitude > MINIMUM_DRAW_STEP)
        {
            path.positionCount++;
            path.SetPosition(path.positionCount - 1, point);
            NotifyWaypointsOfLinePointDrawn(point, node.Color);
            NotifyWarpsOfLinePointDrawn(path, point, node.Color);
        }
    }

    private void NotifyWaypointsOfLinePointDrawn(Vector3 point, int color)
    {
        var cell = Grid.GetLookingAtCell(point.ToPolar());
        if(!WaypointsByGridCell.ContainsKey(cell))
        {
            return;
        }
        var waypoint = WaypointsByGridCell[cell];
        var previouslyReached = waypoint.Reached;
        waypoint.LinePointDrawnInCell(point, color);
        var nowReached = waypoint.Reached;
        if (nowReached && !previouslyReached)
        {
            WaypointReached?.Invoke(waypoint);
            var reachedWaypoints = Waypoints.Count(w => w.Reached);
            var effectToUse = WaypointColoredEffects[Mathf.Clamp(reachedWaypoints - 1, 0, WaypointColoredEffects.Count - 1)];
            Instantiate(effectToUse);
        }
    }

    private void NotifyWarpsOfLinePointDrawn(LineRenderer path, Vector3 point, int color)
    {
        var cell = Grid.GetLookingAtCell(point.ToPolar());
        if (!WarpsByGridCell.ContainsKey(cell))
        {
            return;
        }
        var warp = WarpsByGridCell[cell];
        if(warp.Role == Warp.WarpRole.Open)
        {
            warp.TakeWarp(path, point, color);
            WarpTaken?.Invoke(warp, warp.PairedWarp);
            CameraController.SnapToGridCell(warp.PairedWarp.GridCell);
        }
        //if (previousWaypointColor != newWaypointColor)
        //{
        //    var coloredWaypoints = Waypoints.Count(w => w.Color >= 0);
        //    var effectToUse = WaypointColoredEffects[Mathf.Clamp(coloredWaypoints - 1, 0, WaypointColoredEffects.Count - 1)];
        //    Instantiate(effectToUse);
        //}
    }

    public void NotifyWaypointsOfLinePointRemoved(Vector3 point)
    {
        var cell = Grid.GetLookingAtCell(point.ToPolar());
        if (!WaypointsByGridCell.ContainsKey(cell))
        {
            return;
        }
        var waypoint = WaypointsByGridCell[cell];
        var previouslyReached = waypoint.Reached;
        waypoint.LinePointRemovedFromCell(point);
        var nowReached = waypoint.Reached;
        if (previouslyReached && !nowReached)
        {
            WaypointUnreached?.Invoke(waypoint);
        }
    }

    private void NotifyWarpsOfLinePointRemoved(Vector3 point)
    {
        var cell = Grid.GetLookingAtCell(point.ToPolar());
        if (!WarpsByGridCell.ContainsKey(cell))
        {
            return;
        }
        var warp = WarpsByGridCell[cell];
        bool warpUntaken = warp.LinePointRemovedFromCell(point);
        if(warpUntaken)
        {
            WarpUntaken?.Invoke(warp, warp.PairedWarp);
        }
    }

    public void InitializePuzzle()
    {
        Initialized = false;
        ResetPuzzle();

        var puzzleProvider = PuzzleProvider.Instance;
        if (!puzzleProvider)
        {
            Debug.LogError("No puzzle provider found!");
            return;
        }
        if (!puzzleProvider.PuzzleConfig)
        {
            Debug.LogError("Puzzle provider has no puzzle config!");
            return;
        }
        PuzzleConfig = puzzleProvider.PuzzleConfig;

        Grid.Initialize(PuzzleConfig.GridCellsPerRow, gridVisible: GridVisible);

        PathConnectToNodeDistance = Grid.ClosestDistanceBetweenNeighbors * 0.9f;
        PathCollisionDistance = Grid.ClosestDistanceBetweenNeighbors * 0.4f;
        NodeCollisionDistance = Grid.ClosestDistanceBetweenNeighbors * 0.5f;

        if (PuzzleConfig != null)
        {
            SetupPuzzle(PuzzleConfig);
        }
        Initialized = true;
        PuzzleInitialized?.Invoke();
    }

    private void OnTap(Vector2 tapPosition)
    {
        if(Completed || InputLocks > 0 || ShouldIgnoreInputPosition(tapPosition))
        {
            return;
        }
        var nodeLayerMask = LayerMask.GetMask("Node", "InputCatch");
        var ray = CameraController.Camera.ScreenPointToRay(tapPosition);
        var anyHit = Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, nodeLayerMask);
        if(anyHit)
        {
            var node = hitInfo.collider.GetComponent<Node>();
            if(node)
            {
                OnNodeTapped(node);
            }
            else
            {
                // Selection by line
                Node selectedNodeByLine = null;
                Vector3? tappedPoint = null;

                foreach (var kvp in Paths)
                {
                    var numPoints = kvp.Value.positionCount;
                    var points = new Vector3[numPoints];
                    kvp.Value.GetPositions(points);
                    for(int i = 0; i < numPoints; i++)
                    {
                        if(Vector3.Distance(points[i], hitInfo.point) < kvp.Value.endWidth)
                        {
                            tappedPoint = points[i];
                            break;
                        }
                    }
                    if(tappedPoint.HasValue)
                    {
                        var nodesOfColor = Nodes.Where(n => n.Color == kvp.Key);
                        foreach(var nodeOfColor in nodesOfColor)
                        {
                            if(nodeOfColor.Path == kvp.Value && nodeOfColor.transform.position == kvp.Value.GetPosition(0))
                            {
                                selectedNodeByLine = nodeOfColor;
                                break;
                            }
                        }
                        break;
                    }
                }

                if(selectedNodeByLine)
                {
                    OnPathTapped(selectedNodeByLine, tappedPoint.Value);
                }
                else if (ActiveNode)
                {
                    SetActiveNode(null, fromExistingLine: false);
                    NodeDeselected?.Invoke();
                }
            }
        }
        else if(ActiveNode)
        {
            SetActiveNode(null, fromExistingLine: false);
            NodeDeselected?.Invoke();
        }
    }

    private bool ShouldIgnoreInputPosition(Vector2 position)
    {
        if(IgnoreInputRegions is null)
        {
            return false;
        }
        foreach(var rect in IgnoreInputRegions)
        {
            if(RectTransformUtility.RectangleContainsScreenPoint(rect, position))
            {
                return true;
            }
        }
        return false;
    }
    public void OnPathTapped(Node n, Vector3 tappedPoint)
    {
        if(n.Connected)
        {
            SetDisconnected(n);
        }

        TrimPathToPoint(n.Path, tappedPoint);

        SetActiveNode(n, fromExistingLine: true);
        NodeSelected?.Invoke(n);
    }

    public void OnNodeTapped(Node n)
    {
        SetActiveNode(n, fromExistingLine: false);
        NodeSelected?.Invoke(n);
    }

    public void LockInput() => InputLocks++;
    public void FreeInput() => InputLocks = Mathf.Max(InputLocks - 1, 0);

    public bool IsComplete()
    {
        // Make sure all pairs of nodes are connected
        var nodesAllConnected = Nodes.TrueForAll(n => n.Connected);
        if(!nodesAllConnected)
        {
            return false;
        }
        // Make sure all waypoints have been hit
        var waypointsAllReached = Waypoints.TrueForAll(w => w.Reached);
        if(!waypointsAllReached)
        {
            return false;
        }

        return true;
    }

    private const float PATH_SIZE_RELATIVE_TO_NODE_SIZE = 0.6f;
    public void SetActiveNode(Node n, bool fromExistingLine)
    {
        if(!n)
        {
            if(ActiveNode)
            {
                ActiveNode.Deactivate();
                ActiveNode.PairedNode.Deactivate();
                ActiveNode = null;
            }
            return;
        }

        if(fromExistingLine)
        {
            CameraController.SnapToNodeEndOfPath(n);
        }
        else
        {
            CameraController.SnapToGridCell(n.GridCell);
        }

        if(ActiveNode)
        {
            ActiveNode.Deactivate();
        }
        ActiveNode = n;

        if(!fromExistingLine)
        {
            if (ActiveNode.Path)
            {
                DeleteNodePath(ActiveNode);
            }
            if (ActiveNode.PairedNode.Path)
            {
                DeleteNodePath(ActiveNode.PairedNode);
            }
            Paths.Remove(ActiveNode.Color);
        }
        n.Activate(newPath: !fromExistingLine);
        if(!fromExistingLine)
        {
            Paths[n.Color] = n.Path;
            n.Path.startWidth = n.transform.localScale.x * PATH_SIZE_RELATIVE_TO_NODE_SIZE;
            n.Path.endWidth = n.transform.localScale.x * PATH_SIZE_RELATIVE_TO_NODE_SIZE;
        }
    }

    private void TrimPathToPoint(LineRenderer path, Vector3 point)
    {
        var numPoints = path.positionCount;
        var points = new Vector3[numPoints];
        path.GetPositions(points);
        int i = 0;
        for(; i < numPoints; i++)
        {
            if(points[i] == point)
            {
                break;
            }
        }

        path.positionCount = i + 1;
        i++;
        for(; i< numPoints; i++)
        {
            NotifyWaypointsOfLinePointRemoved(points[i]);
            NotifyWarpsOfLinePointRemoved(points[i]);
        }
    }

    private void DeleteNodePath(Node node)
    {
        SetDisconnected(node);

        if (node.Path)
        {
            for(int i = 0; i < node.Path.positionCount; i++)
            {
                NotifyWaypointsOfLinePointRemoved(node.Path.GetPosition(i));
                NotifyWarpsOfLinePointRemoved(node.Path.GetPosition(i));
            }
            Destroy(node.Path.gameObject);
        }
    }

    public void SetConnected(Node a, Node b)
    {
        a.Connected = true;
        b.Connected = true;
        a.Deactivate();
        b.Deactivate();
        ActiveNode = null;

        NodesConnected?.Invoke(a, b);

        if(IsComplete())
        {
            PuzzleCompleted?.Invoke();
            if(PuzzleCompletionManager.Instance)
            {
                PuzzleCompletionManager.Instance.SetPuzzleCompleted(PuzzleConfig.ID);
            }
            Completed = true;
            if(PuzzleCompleteEffect)
            {
                Instantiate(PuzzleCompleteEffect);
            }
        }
        else if(NodesConnectedEffects.Any())
        {
            var numConnectedPairs = NodesByColor.Count(kvp => kvp.Value.First().Connected);
            var totalPairs = NodesByColor.Count;
            var percentDone = 0f;
            if(totalPairs > 1)
            {
                percentDone = (float)(numConnectedPairs - 1) / (totalPairs - 1);
            }
            var effectToUse = NodesConnectedEffects[Mathf.CeilToInt(Mathf.Lerp(0, NodesConnectedEffects.Count-1, percentDone))];
            Instantiate(effectToUse);
        }
    }

    public void SetDisconnected(Node a)
    {
        var wasConnected = a.Connected;
        a.Connected = false;
        a.PairedNode.Connected = false;
        if(wasConnected)
        {
            NodesDisconnected?.Invoke(a, a.PairedNode);
        }
    }

    private void ResetPuzzle()
    {
        if (Nodes != null)
        {
            foreach (var node in Nodes)
            {
                Destroy(node.gameObject);
            }
            Nodes.Clear();
            NodesByColor.Clear();
        }
        ActiveNode = null;
        if (Paths != null)
        {
            foreach (var kvp in Paths)
            {
                Destroy(kvp.Value.gameObject);
            }
            Paths.Clear();
        }
        if(Waypoints != null)
        {
            foreach(var waypoint in Waypoints)
            {
                Destroy(waypoint.gameObject);
            }
            Waypoints.Clear();
            WaypointsByGridCell.Clear();
        }
        if(Warps != null)
        {
            foreach(var warp in Warps)
            {
                if(warp.WarpPreviewLine)
                {
                    Destroy(warp.WarpPreviewLine.gameObject);
                }
            }
            foreach(var warp in Warps)
            {
                Destroy(warp.gameObject);
            }
            Warps.Clear();
            WarpsByGridCell.Clear();
        }
        if (Walls != null)
        {
            foreach (var wall in Walls)
            {
                Destroy(wall.gameObject);
            }
            Walls.Clear();
        }
        if (Rocks != null)
        {
            foreach (var rock in Rocks)
            {
                Destroy(rock.gameObject);
            }
            Rocks.Clear();
        }

        Grid.Clear();
        Completed = false;
    }

    public void Undo()
    {
        Debug.Log("TODO, undo!");
    }

    public void RevealHint()
    {
        Debug.Log("TODO, reveal hint!");
    }

    #region Setup
    public void SetupPuzzle(PuzzleConfig cfg)
    {
        SetupNodes(cfg);
        SetupWaypoints(cfg);
        SetupWarps(cfg);
        SetupObstacles(cfg);
        SetupView(cfg);
    }

    private const float NODE_VISUAL_SCALE_FACTOR = 2.24f;
    private void SetupNodes(PuzzleConfig cfg)
    {
        ActiveNode = null;
        var children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        Nodes = new List<Node>();
        NodesByColor = new Dictionary<int, List<Node>>();
        Paths = new Dictionary<int, LineRenderer>();

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

    private void SetupWaypoints(PuzzleConfig cfg)
    {
        if(cfg.WaypointPositions is null)
        {
            return;
        }
        for (int i = 0; i < cfg.WaypointPositions.Length; i++)
        {
            var color = cfg.WaypointColors[i];

            var row = cfg.WaypointPositions[i].x;
            var rowCell = cfg.WaypointPositions[i].y;

            var newWaypointGO = Instantiate(WaypointPrefab);
            newWaypointGO.transform.parent = transform;
            newWaypointGO.name = $"Waypoint r{row}c{rowCell}";

            var cell = Grid.CellsByRow[row][rowCell];

            var newWaypoint = newWaypointGO.GetComponent<Waypoint>();
            newWaypoint.SetGridCell(cell);
            newWaypoint.SetColor(color);

            Waypoints.Add(newWaypoint);
            WaypointsByGridCell.Add(cell, newWaypoint);
        }
    }

    private void SetupWarps(PuzzleConfig cfg)
    {
        if (cfg.WarpPositions is null)
        {
            return;
        }

        Warp makeWarp(int row, int rowCell)
        {
            var newWarpGO = Instantiate(WarpPrefab);
            newWarpGO.transform.parent = transform;
            newWarpGO.name = $"Warp r{row}c{rowCell}";

            var cell = Grid.CellsByRow[row][rowCell];

            var newWarp = newWarpGO.GetComponent<Warp>();
            newWarp.SetGridCell(cell);

            Warps.Add(newWarp);
            WarpsByGridCell.Add(cell, newWarp);

            return newWarp;
        }

        for(int i = 0; i < cfg.WarpPositions.Length-1; i += 2)
        {
            var row1 = cfg.WarpPositions[i].x;
            var rowCell1 = cfg.WarpPositions[i].y;
            var row2 = cfg.WarpPositions[i + 1].x;
            var rowCell2 = cfg.WarpPositions[i + 1].y;

            var warp1 = makeWarp(row1, rowCell1);
            var warp2 = makeWarp(row2, rowCell2);

            warp1.SetPairedWarp(warp2);
        }
    }

    private void SetupObstacles(PuzzleConfig cfg)
    {
        SetupRocks(cfg);
        SetupWalls(cfg);
    }

    private void SetupRocks(PuzzleConfig cfg)
    {
        if (cfg.RockPositions is null)
        {
            return;
        }
        for (int i = 0; i < cfg.RockPositions.Length; i++)
        {
            var row = cfg.RockPositions[i].x;
            var rowCell = cfg.RockPositions[i].y;

            var newRockGO = Instantiate(RockPrefab);
            newRockGO.transform.parent = transform;
            newRockGO.name = $"Rock r{row}c{rowCell}";

            var rockPosition = cfg.RockPositions[i];

            var cell = Grid.CellsByRow[rockPosition.x][rockPosition.y];
            newRockGO.transform.position = cell.transform.position;
            newRockGO.transform.LookAt(transform);

            Rocks.Add(newRockGO);
        }
    }

    private void SetupWalls(PuzzleConfig cfg)
    {
        if (cfg.WallPositions is null)
        {
            return;
        }
        for (int i = 0; i < cfg.WallPositions.Length; i++)
        {
            var row = cfg.WallPositions[i].x;
            var rowCell = cfg.WallPositions[i].y;

            var newWallGO = Instantiate(WallPrefab);
            newWallGO.transform.parent = transform;
            newWallGO.name = $"Wall r{row}c{rowCell}";

            var wallPosition = cfg.WallPositions[i];

            var cell = Grid.CellsByRow[wallPosition.x][wallPosition.y];

            var newWall = newWallGO.GetComponent<Wall>();
            newWall.SetGridCell(cell);

            Walls.Add(newWall);
        }
    }

    private void SetupView(PuzzleConfig cfg)
    {
        if(cfg.OpaqueSphere)
        {
            MainSphere.material.SetFloat("_Opacity", 1f);
        }

        // Camera setup
        CameraController.SnapTo(cfg.CameraArmStart, cfg.CameraDistance, cfg.CameraFoV);
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
        foreach (var kvp in Paths)
        {
            if (kvp.Key != excludeColor)
            {
                var path = kvp.Value;
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
        var wallPathCollisionPadding = Mathf.Tan(PathCollisionDistance) * Mathf.Rad2Deg * 0.9f;
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

        var waypointPathCollisionPadding = wallPathCollisionPadding;
        foreach (var waypoint in Waypoints)
        {
            if(waypoint.Color == excludeColor)
            {
                continue;
            }

            // Latitude "minimum" is the top, so it's actually the max
            if (pointPolar.Latitude < waypoint.GridCell.LatitudeMin + waypointPathCollisionPadding && pointPolar.Latitude > waypoint.GridCell.LatitudeMax - waypointPathCollisionPadding &&
                pointPolar.Longitude > waypoint.GridCell.LongitudeMin - waypointPathCollisionPadding && pointPolar.Longitude < waypoint.GridCell.LongitudeMax + waypointPathCollisionPadding)
            {
                return false;
            }
        }
        return true;
    }

    #region Line Smoothing
    public float DISTANCE_TO_ASSUME_WARP = 0.1f;

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

    public void SmoothEndOfLine(LineRenderer renderer, int lineColor)
    {
        var positionCount = renderer.positionCount;
        if(positionCount < 3)
        {
            return;
        }

        var p1 = renderer.GetPosition(positionCount - 3);
        var p2 = renderer.GetPosition(positionCount - 2);
        var p3 = renderer.GetPosition(positionCount - 1);

        if(Vector3.Distance(p1, p2) > DISTANCE_TO_ASSUME_WARP ||
            Vector3.Distance(p2, p3) > DISTANCE_TO_ASSUME_WARP)
        {
            return;
        }

        if(SmoothLineSegment(p1, p2, p3, out Vector3 resultingP2))
        {
            NotifyWaypointsOfLinePointRemoved(p2);
            renderer.SetPosition(positionCount - 2, resultingP2);
            NotifyWaypointsOfLinePointDrawn(resultingP2, lineColor);
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

    public void DejitterEndOfLine(LineRenderer renderer, int lineColor)
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

        if (Vector3.Distance(p1, p2) > DISTANCE_TO_ASSUME_WARP ||
            Vector3.Distance(p2, p3) > DISTANCE_TO_ASSUME_WARP ||
            Vector3.Distance(p3, p4) > DISTANCE_TO_ASSUME_WARP)
        {
            return;
        }

        if (DejitterLineSegment(p1, p2, p3, p4, out Vector3 resultingP2, out Vector3 resultingP3))
        {
            NotifyWaypointsOfLinePointRemoved(p2);
            renderer.SetPosition(positionCount - 3, resultingP2);
            NotifyWaypointsOfLinePointDrawn(resultingP2, lineColor);
            NotifyWaypointsOfLinePointRemoved(p3);
            renderer.SetPosition(positionCount - 2, resultingP3);
            NotifyWaypointsOfLinePointDrawn(resultingP3, lineColor);
        }
    }
	#endregion
}
