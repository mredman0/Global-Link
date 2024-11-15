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

    // Start is called before the first frame update
    void Start()
    {
        BackgroundMaterial = GetComponent<Renderer>().material;
        CurrentForegroundColor = NoNodeColor;
        Puzzle.NodeSelected += OnNodeSelected;
        Puzzle.NodeDeselected += OnNodeDeselected;
    }

    private void OnDestroy()
    {
        Puzzle.NodeSelected -= OnNodeSelected;
        Puzzle.NodeDeselected -= OnNodeDeselected;
    }

    private void OnNodeSelected(Node node)
    {
        var newColor = ColorManager.Instance.ApplyActiveColorMap(node.Color);
        SwitchToColor(newColor);
    }

    private void OnNodeDeselected()
    {
        SwitchToColor(NoNodeColor);
    }

    private void SwitchToColor(Color newColor)
    {
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
