using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForTap : TutorialInstructionStep
{
    public float StartupDelay = 0.5f;

    private float StartTime;
    private bool Tapped = false;

    protected override bool ShouldGoToNextStep() => Tapped;

    // Start is called before the first frame update
    void Start()
    {
        StartTime = Time.time;
        InputManager.Instance.Tap += OnTap;
    }

    void OnDestroy()
    {
        InputManager.Instance.Tap -= OnTap;
    }

    private void OnTap(Vector2 position)
    {
        if(Time.time > StartTime + StartupDelay)
        {
            Tapped = true;
        }
    }
}
