using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForWaypointReached : TutorialInstructionStep
{
    public int Color = -1;

    private bool Reached = false;

    protected override bool ShouldGoToNextStep() => Reached;

    protected override void OnShown()
    {
        Puzzle.WaypointReached += OnWaypointReached;
    }

    protected override void OnHidden()
    {
        Puzzle.WaypointUnreached -= OnWaypointReached;
    }

    private void OnWaypointReached(Waypoint waypoint)
    {
        Reached = Color < 0 || waypoint.Color == Color;
    }
}
