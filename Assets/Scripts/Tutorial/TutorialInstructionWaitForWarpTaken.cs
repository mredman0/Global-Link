using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForWarpTaken : TutorialInstructionStep
{
    private bool WarpTaken = false;

    protected override bool ShouldGoToNextStep() => WarpTaken;

    private void Start()
    {
        Puzzle.WarpTaken += OnWarpTaken;
    }

    private void OnDestroy()
    {
        Puzzle.WarpTaken -= OnWarpTaken;
    }

    private void OnWarpTaken(Warp warp, Warp pairedWarp)
    {
        WarpTaken = true;
    }
}
