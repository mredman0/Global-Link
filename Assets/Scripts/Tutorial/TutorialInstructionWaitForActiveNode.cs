using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForActiveNode : TutorialInstructionStep
{
    protected override bool ShouldGoToNextStep() => Puzzle.ActiveNode;
}
