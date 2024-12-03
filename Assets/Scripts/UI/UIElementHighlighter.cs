using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIElementHighlighter : MonoBehaviour
{
    public RectTransform Target;

    public RectTransform[] Blockers;

    // Start is called before the first frame update
    void Start()
    {
        ConfigureBlockers();
    }

    public void Show()
    {
        if(!Target)
        {
            return;
        }

        foreach(var blocker in Blockers)
        {
            blocker.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        foreach (var blocker in Blockers)
        {
            blocker.gameObject.SetActive(false);
        }
    }

    public void ConfigureBlockers()
    {
        if(!Target)
        {
            foreach(var blocker in Blockers)
            {
                blocker.gameObject.SetActive(false);
            }
            return;
        }
        var canvas = Target.GetComponentInParent<Canvas>();

        var corners = GetScreenSpaceCorners(Target);
        var left = corners[0].x / canvas.scaleFactor;
        var right = corners[2].x / canvas.scaleFactor;
        var bottom = corners[0].y / canvas.scaleFactor;
        var top = corners[2].y / canvas.scaleFactor;

        var bLeft = Blockers[0];
        var bRight = Blockers[1];
        var bTop = Blockers[2];
        var bBottom = Blockers[3];

        var width = Screen.width / canvas.scaleFactor;
        var height = Screen.height / canvas.scaleFactor;

        bLeft.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, left);
        bLeft.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, height);

        bRight.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, width - right);
        bRight.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, height);

        bTop.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, left, right - left);
        bTop.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, height - top);

        bBottom.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, left, right - left);
        bBottom.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, bottom);
    }

    private static Vector2[] GetScreenSpaceCorners(RectTransform target)
    {
        // Get world corners of the RectTransform
        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners);
        return worldCorners.Select(w => new Vector2(w.x, w.y)).ToArray();
    }
}
