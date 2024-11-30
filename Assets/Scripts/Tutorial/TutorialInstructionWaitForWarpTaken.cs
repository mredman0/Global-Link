using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForWarpTaken : TutorialInstructionStep
{
    private bool WarpTaken = false;

    protected override bool ShouldGoToNextStep() => WarpTaken;

    protected override void OnShown()
    {
        Puzzle.WarpTaken += OnWarpTaken;
    }

    protected override void OnHidden()
    {
        Puzzle.WarpTaken -= OnWarpTaken;
    }

    private void OnWarpTaken(Warp warp, Warp pairedWarp)
    {
        WarpTaken = true;
    }
}
