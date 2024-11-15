using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NodesUI : MonoBehaviour
{
    [Header("Required References")]
    public Puzzle Puzzle;

    public HorizontalLayoutGroup Row1;
    public HorizontalLayoutGroup Row2;

    public GameObject NodeDisplayPrefab;

    [Header("Settings")]
    public int ColorsPerRow = 3;

    private Dictionary<int, NodePairDisplay> NodeDisplays = new Dictionary<int, NodePairDisplay>();

    // Start is called before the first frame update
    void Start()
    {
        Puzzle.PuzzleInitialized += OnPuzzleInitialized;
        if (Puzzle.Initialized)
        {
            OnPuzzleInitialized();
        }

        Puzzle.NodeSelected += OnNodeSelected;
        Puzzle.NodeDeselected += OnNodeDeselected;
        Puzzle.NodesConnected += OnNodesConnected;
        Puzzle.NodesDisconnected += OnNodesDisconnected;
        Puzzle.WaypointReached += OnWaypointReached;
        Puzzle.WaypointUnreached += OnWaypointUnreached;
    }

    private void OnDestroy()
    {
        Puzzle.PuzzleInitialized -= OnPuzzleInitialized;

        Puzzle.NodeSelected -= OnNodeSelected;
        Puzzle.NodeDeselected -= OnNodeDeselected;
        Puzzle.NodesConnected -= OnNodesConnected;
        Puzzle.NodesDisconnected -= OnNodesDisconnected;
        Puzzle.WaypointReached -= OnWaypointReached;
        Puzzle.WaypointUnreached -= OnWaypointUnreached;
    }

    private void OnPuzzleInitialized()
    {
        foreach (var kvp in NodeDisplays)
        {
            Destroy(kvp.Value.gameObject);
        }
        NodeDisplays.Clear();

        var colorsAdded = 0;
        foreach (var kvp in Puzzle.NodesByColor)
        {
            var color = kvp.Key;

            var row = colorsAdded < ColorsPerRow ? Row1 : Row2;
            var nodesDisplayGO = Instantiate(NodeDisplayPrefab, row.transform);
            var nodesDisplay = nodesDisplayGO.GetComponent<NodePairDisplay>();
            nodesDisplay.SetColorAndHasWaypoint(color, Puzzle.Waypoints.Any(w => w.Color == color));

            NodeDisplays.Add(color, nodesDisplayGO.GetComponent<NodePairDisplay>());
            colorsAdded++;
        }
    }

    private void OnNodeSelected(Node n)
    {
        foreach (var kvp in NodeDisplays)
        {
            kvp.Value.SetColorSelected(false);
        }
        NodeDisplays[n.Color].SetColorSelected(true);
    }
    private void OnNodeDeselected()
    {
        foreach(var kvp in NodeDisplays)
        {
            kvp.Value.SetColorSelected(false);
        }
    }

    private void OnNodesConnected(Node a, Node b)
    {
        NodeDisplays[a.Color].SetNodesConnected(true);
    }
    private void OnNodesDisconnected(Node a, Node b)
    {
        NodeDisplays[a.Color].SetNodesConnected(false);
    }

    private void OnWaypointReached(Waypoint waypoint)
    {
        NodeDisplays[waypoint.Color].SetWaypointReached(Puzzle.Waypoints.Where(w => w.Color == waypoint.Color).All(w => w.Reached));
    }

    private void OnWaypointUnreached(Waypoint waypoint)
    {
        NodeDisplays[waypoint.Color].SetWaypointReached(false);
    }
}
