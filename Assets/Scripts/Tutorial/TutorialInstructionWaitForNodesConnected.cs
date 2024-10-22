using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForNodesConnected : TutorialInstructionStep
{
    public int Color = -1;

    private bool Connected = false;

    protected override bool ShouldGoToNextStep() => Connected;

    private void Start()
    {
        Puzzle.NodesConnected += OnNodesConnected;
    }

    private void OnDestroy()
    {
        Puzzle.NodesConnected -= OnNodesConnected;
    }

    private void OnNodesConnected(Node a, Node b)
    {
        Connected = Color < 0 || a.Color == Color;
    }
}
