using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaypointsUI : MonoBehaviour
{
    [Header("Required References")]
    public Puzzle Puzzle;

    public HorizontalLayoutGroup Row1;
    public HorizontalLayoutGroup Row2;

    public GameObject WaypointDisplayPrefab;

    private Dictionary<Waypoint, Animator> WaypointDisplayAnimators = new Dictionary<Waypoint, Animator>();

    // Start is called before the first frame update
    void Start()
    {
        Puzzle.PuzzleInitialized += OnPuzzleInitialized;
        if (Puzzle.Initialized)
        {
            OnPuzzleInitialized();
        }

        Puzzle.WaypointColored += OnWaypointColored;
        Puzzle.WaypointUncolored += OnWaypointUncolored;
    }

    private void OnDestroy()
    {
        Puzzle.PuzzleInitialized -= OnPuzzleInitialized;
        Puzzle.WaypointColored -= OnWaypointColored;
        Puzzle.WaypointUncolored -= OnWaypointUncolored;
    }

    private void OnPuzzleInitialized()
    {
        foreach(var kvp in WaypointDisplayAnimators)
        {
            Destroy(kvp.Value.gameObject);
        }
        WaypointDisplayAnimators.Clear();
        int waypointsRendered = 0;
        foreach(var waypoint in Puzzle.Waypoints)
        {
            var waypointDisplayGO = Instantiate(WaypointDisplayPrefab, waypointsRendered < 3 ? Row1.transform : Row2.transform);
            waypointsRendered++;
            var waypointDisplay = waypointDisplayGO.GetComponent<Animator>();
            WaypointDisplayAnimators.Add(waypoint, waypointDisplay);
        }
    }

    private void OnWaypointColored(Waypoint waypoint)
    {
        WaypointDisplayAnimators[waypoint].SetInteger("ColorIndex", waypoint.Color);
    }

    private void OnWaypointUncolored(Waypoint waypoint)
    {
        WaypointDisplayAnimators[waypoint].SetInteger("ColorIndex", -1);
    }
}
