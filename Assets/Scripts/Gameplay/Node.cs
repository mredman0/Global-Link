using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node : MonoBehaviour
{
    public List<Material> ColorIconMaterials;

    public Puzzle Puzzle;
    public int Color;
    public bool Connected = false;
    public bool Active = false;

    public GameObject PathPrefab;
    public MultiLineRenderer Path;

    public GameObject Ball;
    public GameObject ColorIconArm;
    public MeshRenderer ColorIconRenderer;

    public MeshFilter CellOutlinerMesh;
    public float VerticesPerDegree = 0.5f;

    public GridCell GridCell;

    public Node PairedNode { get; private set; }
    public float VisualScale = 1f;

    // Start is called before the first frame update
    void Start()
    {
        ColorIconArm.SetActive(SettingsManager.Instance.GetBool(SHOW_COLOR_ICONS_KEY));
        SettingsManager.Instance.BoolSettingChanged += OnBoolSettingChanged;
    }

    private void OnDestroy()
    {
        SettingsManager.Instance.BoolSettingChanged -= OnBoolSettingChanged;
    }

    private const string SHOW_COLOR_ICONS_KEY = "AccessibilityShowColorIcons";
    private void OnBoolSettingChanged(string setting, bool value)
    {
        if (setting == SHOW_COLOR_ICONS_KEY)
        {
            ColorIconArm.SetActive(value);
        }
    }

    public void StartPath()
    {
        Path = Instantiate(PathPrefab).GetComponent<MultiLineRenderer>();

        var pathColor = ColorManager.Instance.GetColor(Color);
        Path.Color = pathColor;
        if (Puzzle)
        {
            Path.transform.parent = Puzzle.transform;
        }

        Path.StartNewLine();
        Path.PositionCount++;
        Path.SetPosition(0, transform.position);
    }

    public void Initialize(Puzzle puzzle, int color, GridCell cell)
    {
        Puzzle = puzzle;
        SetColor(color);
        GridCell = cell;

        transform.position = cell.transform.position;

        var radiusMultiplier = 0.95f;
        var latitudeSegments = Mathf.CeilToInt(VerticesPerDegree * Mathf.Abs(cell.LatitudeMin - cell.LatitudeMax));
        var longitudeSegments = Mathf.CeilToInt(VerticesPerDegree * Mathf.Abs(cell.LongitudeMin - cell.LongitudeMax));
        CellOutlinerMesh.sharedMesh = MeshGenerator.GenerateSphereSectorRounded(cell.LatitudeMin, cell.LatitudeMax, cell.LongitudeMin, cell.LongitudeMax, radiusMultiplier, latitudeSegments, longitudeSegments, 0.25f);
        CellOutlinerMesh.transform.localPosition = transform.localPosition * -1f;
    }

    public void SetColor(int color)
    {
        Color = color;
        var mappedColor = ColorManager.Instance.GetColor(color);
        Ball.GetComponent<Renderer>().material.SetColor("_Color", mappedColor);
        CellOutlinerMesh.GetComponent<Renderer>().material.SetColor("_Color", mappedColor);
        if(Path)
        {
            Path.Color = mappedColor;
        }
        ColorIconRenderer.material = ColorIconMaterials[Color];
    }

    public void SetVisualScale(float scale)
    {
        VisualScale = scale;
        Ball.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void SetPairedNode(Node other)
    {
        PairedNode = other;
    }

    public void SetSelected()
    {
        Ball.GetComponent<Animator>().SetBool("Selected", true);
    }
    public void SetDeselected()
    {
        Ball.GetComponent<Animator>().SetBool("Selected", false);
    }
}
