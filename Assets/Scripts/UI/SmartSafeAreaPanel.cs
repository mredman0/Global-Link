using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartSafeAreaPanel : MonoBehaviour
{
    public enum SafeAreaEdge { Top, Bottom, Both }
    public enum AdjustMode { Resize, Move }

    public SafeAreaEdge edgeToAdjust;
    public AdjustMode mode;

    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        AdjustBasedOnSafeArea();
    }

    private void AdjustBasedOnSafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        float topUnsafeArea = screenSize.y - (safeArea.y + safeArea.height);
        float bottomUnsafeArea = safeArea.y;

        switch (mode)
        {
            case AdjustMode.Resize:
                ResizeEdge(topUnsafeArea, bottomUnsafeArea);
                break;
            case AdjustMode.Move:
                MoveEdge(topUnsafeArea, bottomUnsafeArea);
                break;
        }
    }

    private void ResizeEdge(float topUnsafeArea, float bottomUnsafeArea)
    {
        switch (edgeToAdjust)
        {
            case SafeAreaEdge.Top:
                AdjustBottomUpward(topUnsafeArea*-1);
                break;
            case SafeAreaEdge.Bottom:
                AdjustTopDownward(bottomUnsafeArea*-1);
                break;
            case SafeAreaEdge.Both:
                AdjustTopDownward(topUnsafeArea);
                AdjustBottomUpward(bottomUnsafeArea);
                break;
        }
    }

    private void MoveEdge(float topUnsafeArea, float bottomUnsafeArea)
    {
        Vector3 position = rectTransform.localPosition;
        switch (edgeToAdjust)
        {
            case SafeAreaEdge.Top:
                position.y -= topUnsafeArea / 2;  // Move down
                break;
            case SafeAreaEdge.Bottom:
                position.y += bottomUnsafeArea / 2;  // Move up
                break;
            case SafeAreaEdge.Both:
                Debug.LogError($"SafeAreaPanel set to BOTH and MOVE, which doesn't make sense");
                break;
        }
        rectTransform.localPosition = position;
    }

    private void AdjustTopDownward(float amount)
    {
        rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, rectTransform.offsetMax.y - amount);
    }

    private void AdjustBottomUpward(float amount)
    {
        rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, rectTransform.offsetMin.y + amount);
    }
}
