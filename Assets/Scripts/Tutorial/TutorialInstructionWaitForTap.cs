using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionWaitForTap : TutorialInstructionStep
{
    public float StartupDelay = 0.2f;
    public UIElementHighlighter UIHighlighter;
    public string UIHighlightTargetName;

    private float StartTime;
    private bool Tapped = false;

    protected override bool ShouldGoToNextStep() => Tapped;

    protected override void OnShown()
    {
        StartTime = Time.time;
        InputManager.Instance.Tap += OnTap;
        if(UIHighlighter)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == UIHighlightTargetName)
                {
                    var rect = obj.GetComponent<RectTransform>();
                    if(rect)
                    {
                        UIHighlighter.Target = rect;
                        break;
                    }
                }
            }
            UIHighlighter.ConfigureBlockers();
            UIHighlighter.Show();
        }
    }

    protected override void OnHidden()
    {
        InputManager.Instance.Tap -= OnTap;
        if(UIHighlighter)
        {
            UIHighlighter.Hide();
        }
    }

    private void OnTap(Vector2 position)
    {
        if(Time.time > StartTime + StartupDelay)
        {
            Tapped = true;
        }
    }
}
