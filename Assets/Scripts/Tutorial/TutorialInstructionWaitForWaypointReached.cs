using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForWaypointReached : TutorialInstructionStep
{
    public int Color = -1;

    private bool Reached = false;

    protected override bool ShouldGoToNextStep() => Reached;

    private void Start()
    {
        Puzzle.WaypointReached += OnWaypointReached;
    }

    private void OnDestroy()
    {
        Puzzle.WaypointUnreached -= OnWaypointReached;
    }

    private void OnWaypointReached(Waypoint waypoint)
    {
        Reached = Color < 0 || waypoint.Color == Color;
    }
}
