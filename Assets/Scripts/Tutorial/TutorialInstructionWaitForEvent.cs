using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForEvent : TutorialInstructionStep
{
    [Header("Events")]
    public bool WaitForTap;
    public bool WaitForActiveNode;
    public bool WaitForNodesConnected;
    public bool WaitForUndo;
    public bool WaitForReset;
    public bool WaitForWarpTaken;
    public bool WaitForWaypointReached;
    public bool WaitForPuzzleCompelete;

    [Header("Specific Settings")]
    public float TapDelay = 0.2f;
    public int ActiveNodeColor = -1;
    public int NodesConnectedColor = -1;
    public int WaypointColor = -1;

    [Header("UI Highlighting")]
    public UIElementHighlighter UIHighlighter;
    public string UIHighlightTargetName;

    [Header("State")]
    public bool EventHit;
    public float StartTime;

    protected override bool ShouldGoToNextStep() => EventHit;

    protected override void OnShown()
    {
        StartTime = Time.time;

        if (UIHighlighter)
        {
            DoUIHighlight();
        }

        if (WaitForTap)
        {
            InputManager.Instance.Tap += OnTap;
        }
        if(WaitForActiveNode)
        {
            Puzzle.NodeSelected += OnNodeSelected;
        }
        if(WaitForNodesConnected)
        {
            Puzzle.NodesConnected += OnNodesConnected;
        }
        if(WaitForUndo)
        {
            Puzzle.UndoUsed += OnUndoUsed;
        }
        if(WaitForReset)
        {
            Puzzle.ResetUsed += OnResetUsed;
        }
        if(WaitForWarpTaken)
        {
            Puzzle.WarpTaken += OnWarpTaken;
        }
        if(WaitForWaypointReached)
        {
            Puzzle.WaypointReached += OnWaypointReached;
        }
        if(WaitForPuzzleCompelete)
        {
            Puzzle.PuzzleCompleted += OnPuzzleCompleted;
        }
    }

    protected override void OnHidden()
    {
        if (UIHighlighter)
        {
            UIHighlighter.Hide();
        }

        if (WaitForTap)
        {
            InputManager.Instance.Tap -= OnTap;
        }
        if (WaitForActiveNode)
        {
            Puzzle.NodeSelected -= OnNodeSelected;
        }
        if (WaitForNodesConnected)
        {
            Puzzle.NodesConnected -= OnNodesConnected;
        }
        if (WaitForUndo)
        {
            Puzzle.UndoUsed -= OnUndoUsed;
        }
        if (WaitForReset)
        {
            Puzzle.ResetUsed -= OnResetUsed;
        }
        if (WaitForWarpTaken)
        {
            Puzzle.WarpTaken -= OnWarpTaken;
        }
        if (WaitForWaypointReached)
        {
            Puzzle.WaypointReached -= OnWaypointReached;
        }
        if (WaitForPuzzleCompelete)
        {
            Puzzle.PuzzleCompleted -= OnPuzzleCompleted;
        }
    }

    private void OnTap(Vector2 position)
    {
        if (Time.time > StartTime + TapDelay)
        {
            EventHit |= true;
        }
    }
    private void OnNodeSelected(Node selected)
    {
        EventHit |= ActiveNodeColor < 0 || selected.Color == ActiveNodeColor;
    }
    private void OnNodesConnected(Node a, Node b)
    {
        EventHit |= NodesConnectedColor < 0 || a.Color == NodesConnectedColor;
    }
    private void OnUndoUsed()
    {
        EventHit |= true;
    }
    private void OnResetUsed()
    {
        EventHit |= true;
    }
    private void OnWarpTaken(Warp warp, Warp pairedWarp)
    {
        EventHit |= true;
    }
    private void OnWaypointReached(Waypoint waypoint)
    {
        EventHit |= WaypointColor < 0 || waypoint.Color == WaypointColor;
    }
    private void OnPuzzleCompleted()
    {
        EventHit |= true;
    }

    private void DoUIHighlight()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == UIHighlightTargetName)
            {
                var rect = obj.GetComponent<RectTransform>();
                if (rect)
                {
                    UIHighlighter.Target = rect;
                    break;
                }
            }
        }
        UIHighlighter.ConfigureBlockers();
        UIHighlighter.Show();
    }
}
