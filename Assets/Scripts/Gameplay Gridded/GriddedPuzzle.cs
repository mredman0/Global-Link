using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GriddedPuzzle : MonoBehaviour
{
    public static GriddedPuzzle Current { get; set; }

    public GameObject NodePrefab;

    public PuzzleGrid Grid;

    public List<Node> Nodes;
    public Dictionary<int, List<Node>> NodesByColor;
    public Node ActiveNode;

    public List<(int, LineRenderer)> Paths;

    public Camera PuzzleViewCamera;
    public CameraMotorUpright PuzzleCameraMotor;

    public float PanInsteadOfSelectionThreshold = 1f;

    public bool Panning { get; private set; }

    public GriddedPuzzleConfig DEBUG_PUZZLE_CONFIG;

    // Start is called before the first frame update
    void Start()
    {
        Grid.Initialize(DEBUG_PUZZLE_CONFIG.GridCellsPerRow);

        Current = this;
        if (DEBUG_PUZZLE_CONFIG != null)
        {
            SetUpPuzzle(DEBUG_PUZZLE_CONFIG);
        }
        ColorMapController.Instance.ColorMapChanged += OnColorMapChanged;
    }

    private void OnDestroy()
    {
        ColorMapController.Instance.ColorMapChanged -= OnColorMapChanged;
    }

    private void OnColorMapChanged()
    {
        foreach (var node in Nodes)
        {
            node.SetColor(node.Color);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (ActiveNode)
            {
                var lookingAtCell = Grid.GetLookingAtCell(PuzzleViewCamera.transform.position.ToPolar());
                if(!lookingAtCell.Color.HasValue)
                {
                    ActiveNode.AddCellToPath(lookingAtCell);
                }
                else if(lookingAtCell.Color == ActiveNode.Color)
                {
                    bool added = ActiveNode.AddCellToPath(lookingAtCell);
                    if (added && ActiveNode.PairedNode.GridCell == lookingAtCell)
                    {
                        // Connect!
                        if (ActiveNode.PairedNode.Path)
                        {
                            Destroy(ActiveNode.PairedNode.Path.gameObject);
                        }
                        Paths.RemoveAll(p => !p.Item2);
                        ActiveNode.PairedNode.Path = ActiveNode.Path;
                        SetConnected(ActiveNode, ActiveNode.PairedNode);
                    }
                }
                else
                {
                    // Cut existing path at this node, then replace, unless this is a node cell
                    int existingPathColor = lookingAtCell.Color.Value;
                    var nodesOfOtherColor = NodesByColor[existingPathColor];
                    var isOtherNode = false;
                    foreach(var node in nodesOfOtherColor)
                    {
                        if (node.GridCell == lookingAtCell)
                        {
                            isOtherNode = true;
                        }
                    }
                    if(!isOtherNode)
                    {
                        foreach (var node in nodesOfOtherColor)
                        {
                            if (node.GridPath.Contains(lookingAtCell))
                            {
                                node.CutCellFromPath(lookingAtCell);
                            }
                        }
                        ActiveNode.AddCellToPath(lookingAtCell);
                    }
                }
            }
        }
    }

    private void OnMouseDrag()
    {
        Panning = true;
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
    public void SetActiveNode(Node n)
    {
        if (!n)
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
    }

    public void SetConnected(Node a, Node b)
    {
        var pathLine = new Vector3[ActiveNode.Path.positionCount];
        ActiveNode.Path.GetPositions(pathLine);
        //SmoothLine(ref pathLine);
        ActiveNode.Path.SetPositions(pathLine);

        a.Connected = true;
        b.Connected = true;
        a.Deactivate();
        b.Deactivate();
        ActiveNode = null;
    }

    public void SetUpPuzzle(GriddedPuzzleConfig cfg)
    {
        ActiveNode = null;
        var children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        Nodes = new List<Node>();
        NodesByColor = new Dictionary<int, List<Node>>();
        Paths = new List<(int, LineRenderer)>();

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
            newNode.GriddedPuzzle = this;

            var nodePosition = cfg.NodePositions[i];

            var cell = Grid.CellsByRow[nodePosition.x][nodePosition.y];
            newNode.GridCell = cell;
            cell.Color = colorIndex;
            newNode.transform.position = cell.transform.position;

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


    #region Autoconnect
    public void AutoConnectColor(int color)
    {
        var nodes = NodesByColor[color];
        if (nodes[0].Connected)
        {
            return;
        }

        nodes[0].AutoConnectToPairedNode();
    }
    #endregion
}
