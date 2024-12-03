using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIElementHighlighter : MonoBehaviour
{
    public RectTransform Target;

    public RectTransform TopRect, BottomRect, LeftRect, RightRect;
    public Canvas Canvas;

    // Start is called before the first frame update
    void Start()
    {
        Canvas = TopRect.GetComponentInParent<Canvas>();
        ConfigureBlockers();
    }

    public void Show()
    {
        if(!Target)
        {
            return;
        }

        TopRect.gameObject.SetActive(true);
        BottomRect.gameObject.SetActive(true);
        LeftRect.gameObject.SetActive(true);
        RightRect.gameObject.SetActive(true);
    }

    public void Hide()
    {
        TopRect.gameObject.SetActive(false);
        BottomRect.gameObject.SetActive(false);
        LeftRect.gameObject.SetActive(false);
        RightRect.gameObject.SetActive(false);
    }

    public void ConfigureBlockers()
    {
        if(!Target)
        {
            return;
        }

        // Get the canvas RectTransform
        RectTransform canvasRect = Canvas.GetComponent<RectTransform>();

        // Get the bounds of the target RectTransform in canvas space
        Vector3[] targetCorners = new Vector3[4];
        Target.GetWorldCorners(targetCorners);

        // Convert world space corners to canvas space
        Vector3 bottomLeft = canvasRect.InverseTransformPoint(targetCorners[0]);
        Vector3 topRight = canvasRect.InverseTransformPoint(targetCorners[2]);

        // Canvas dimensions
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // Target bounds in canvas space
        float targetXMin = bottomLeft.x + canvasWidth / 2;
        float targetYMin = bottomLeft.y + canvasHeight / 2;
        float targetXMax = topRight.x + canvasWidth / 2;
        float targetYMax = topRight.y + canvasHeight / 2;

        // Configure the RectTransforms
        ConfigureRectTransform(TopRect, 0, targetYMax, canvasWidth, canvasHeight - targetYMax); // Top
        ConfigureRectTransform(BottomRect, 0, 0, canvasWidth, targetYMin); // Bottom
        ConfigureRectTransform(LeftRect, 0, targetYMin, targetXMin, targetYMax - targetYMin); // Left
        ConfigureRectTransform(RightRect, targetXMax, targetYMin, canvasWidth - targetXMax, targetYMax - targetYMin); // Right
    }

    private void ConfigureRectTransform(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0, 0); // Bottom-left corner
        rect.anchorMax = new Vector2(0, 0); // Bottom-left corner
        rect.pivot = new Vector2(0, 0); // Bottom-left corner
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
