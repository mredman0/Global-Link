using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForWaypointColored : TutorialInstructionStep
{
    public int Color = -1;

    private bool Reached = false;

    protected override bool ShouldGoToNextStep() => Reached;

    private void Start()
    {
        Puzzle.WaypointColored += OnWaypointColored;
    }

    private void OnDestroy()
    {
        Puzzle.WaypointColored -= OnWaypointColored;
    }

    private void OnWaypointColored(Waypoint waypoint)
    {
        Reached = Color < 0 || waypoint.Color == Color;
    }
}
