using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleBackgroundController : MonoBehaviour
{
    public Puzzle Puzzle;
    public Color NoNodeColor;

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
    }
}
