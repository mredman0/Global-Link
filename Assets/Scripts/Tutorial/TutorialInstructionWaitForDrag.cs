using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForDrag : TutorialInstructionStep
{
    public float MinDragAmount = 10f;
    public float DelayAfterDrag = 1f;

    public float AmountDragged = 0;
    private bool Dragged = false;
    private float DraggedTime = 0;

    protected override bool ShouldGoToNextStep() => Dragged && (Time.time > DraggedTime + DelayAfterDrag);

    // Start is called before the first frame update
    void Start()
    {
        InputManager.Instance.Drag += OnDrag;
    }

    void OnDestroy()
    {
        InputManager.Instance.Drag -= OnDrag;
    }

    private void OnDrag(Vector2 motion)
    {
        AmountDragged += motion.magnitude;
        if(!Dragged && AmountDragged >= MinDragAmount)
        {
            Dragged = true;
            DraggedTime = Time.time;
        }
    }
}
