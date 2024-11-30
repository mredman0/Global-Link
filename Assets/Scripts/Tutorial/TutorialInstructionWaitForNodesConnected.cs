using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForNodesConnected : TutorialInstructionStep
{
    public int Color = -1;

    private bool Connected = false;

    protected override bool ShouldGoToNextStep() => Connected;

    protected override void OnShown()
    {
        Puzzle.NodesConnected += OnNodesConnected;
    }

    protected override void OnHidden()
    {
        Puzzle.NodesConnected -= OnNodesConnected;
    }

    private void OnNodesConnected(Node a, Node b)
    {
        Connected = Color < 0 || a.Color == Color;
    }
}
