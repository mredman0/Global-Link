using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleBackgroundController : MonoBehaviour
{
    public Puzzle Puzzle;
    public Color NoNodeColor;

    public float TintBaseAlpha = 0.03921569f;
    public float TintBaseAlphaLuma = 0.7152f;

    private Material BackgroundMaterial;
    private Color CurrentForegroundColor;
    private int CurrentColorIndex;

    // Start is called before the first frame update
    void Start()
    {
        BackgroundMaterial = GetComponent<Renderer>().material;
        CurrentForegroundColor = NoNodeColor;
        CurrentColorIndex = -1;
        Puzzle.NodeSelected += OnNodeSelected;
        Puzzle.NodeDeselected += OnNodeDeselected;
        Puzzle.ResetUsed += OnNodeDeselected;

        ColorManager.Instance.ColorSchemeChanged += OnColorSchemeChanged;
    }

    private void OnDestroy()
    {
        Puzzle.NodeSelected -= OnNodeSelected;
        Puzzle.NodeDeselected -= OnNodeDeselected;
        Puzzle.ResetUsed -= OnNodeDeselected;

        ColorManager.Instance.ColorSchemeChanged -= OnColorSchemeChanged;
    }

    private void OnNodeSelected(Node node)
    {
        SwitchToColor(node.Color);
    }

    private void OnNodeDeselected()
    {
        SwitchToColor(-1);
    }

    private void OnColorSchemeChanged()
    {
        SwitchToColor(CurrentColorIndex);
        SwitchToColor(CurrentColorIndex);
    }

    private void SwitchToColor(int colorIndex)
    {
        CurrentColorIndex = colorIndex;
        var newColor = NoNodeColor;
        if(colorIndex >= 0)
        {
            newColor = ColorManager.Instance.GetColor(colorIndex);
        }

        BackgroundMaterial.SetColor("_BackgroundColor", CurrentForegroundColor);
        BackgroundMaterial.SetColor("_ForegroundColor", newColor);
        CurrentForegroundColor = newColor;
        BackgroundMaterial.SetFloat("_StartTime", Time.time);

        var luma = GetLuma(newColor);
        if(luma == 0)
        {
            luma = TintBaseAlphaLuma;
        }
        BackgroundMaterial.SetColor("_Tint", new Color(1f, 1f, 1f, TintBaseAlpha * Mathf.Pow(TintBaseAlphaLuma / luma, 0.25f)));
    }

    private float GetLuma(Color color) =>
        0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
}
