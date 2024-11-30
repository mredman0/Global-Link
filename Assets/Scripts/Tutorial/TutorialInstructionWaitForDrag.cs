using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForDrag : TutorialInstructionStep
{
    public float StartDelay = 10f;
    public float ReduceDelayPerDrag = 1f;

    public float AmountDragged = 0;
    public float CurrentDelay;
    public bool Dragged = false;

    protected override bool ShouldGoToNextStep() => Dragged && (CurrentDelay <= 0);

    protected override void OnShown()
    {
        InputManager.Instance.Drag += OnDrag;
    }

    protected override void OnHidden()
    {
        InputManager.Instance.Drag -= OnDrag;
    }

    private void OnDrag(Vector2 motion)
    {
        if(!Dragged)
        {
            Dragged = true;
            CurrentDelay = StartDelay;
        }

        CurrentDelay -= motion.magnitude * ReduceDelayPerDrag;
    }

    protected new void Update()
    {
        base.Update();

        if(Dragged)
        {
            CurrentDelay -= Time.deltaTime;
        }
    }
}
