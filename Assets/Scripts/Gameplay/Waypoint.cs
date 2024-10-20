using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [Header("Required References")]
    public MeshFilter MeshFilter;
    public MeshRenderer MeshRenderer;
    public Animator Animator;

    [Header("Settings")]
    public int Color;
    public float VerticesPerDegree = 0.5f;

    [Header("State")]
    public bool Reached;

    public GridCell GridCell { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        //MeshRenderer.material = new Material(MeshRenderer.material);
    }

    private float SetColorInterval = 0.5f;
    private float NextColorChange = 0f;
    void Update()
    {
        if(Time.time > NextColorChange)
        {
            NextColorChange = Time.time + SetColorInterval + Random.Range(0f, 1f);
            Color++;
            if(Color > 1)
            {
                Color = -1;
            }
            SetColor(Color);
        }
    }

    public void SetColor(int colorIndex)
    {
        Color = colorIndex;
        Animator.SetInteger("ColorIndex", colorIndex);
    }

    public void SetGridCell(GridCell cell)
    {
        GridCell = cell;
        var latitudeSegments = Mathf.CeilToInt(VerticesPerDegree * Mathf.Abs(cell.LatitudeMin - cell.LatitudeMax));
        var longitudeSegments = Mathf.CeilToInt(VerticesPerDegree * Mathf.Abs(cell.LongitudeMin - cell.LongitudeMax));
        MeshFilter.sharedMesh = MeshGenerator.GenerateSphereSectorRounded(cell.LatitudeMin, cell.LatitudeMax, cell.LongitudeMin, cell.LongitudeMax, 1f, latitudeSegments, longitudeSegments, 0.25f);
    }
}
