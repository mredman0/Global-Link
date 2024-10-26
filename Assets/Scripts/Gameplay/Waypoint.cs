using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    private List<Vector3> DrawnPointsInCell = new List<Vector3>();

    // Start is called before the first frame update
    void Start()
    {

    }

    void Update()
    {

    }

    private void SetColor(int colorIndex)
    {
        Color = colorIndex;
        Animator.SetInteger("ColorIndex", colorIndex);
    }

    public void LinePointDrawnInCell(Vector3 point, int color)
    {
        if(!DrawnPointsInCell.Any())
        {
            SetColor(color);
        }
        DrawnPointsInCell.Add(point);
    }

    public void LinePointRemovedFromCell(Vector3 point)
    {
        DrawnPointsInCell.Remove(point);
        if(!DrawnPointsInCell.Any())
        {
            SetColor(-1);
        }
    }

    public void SetGridCell(GridCell cell)
    {
        GridCell = cell;
        var latitudeSegments = Mathf.CeilToInt(VerticesPerDegree * Mathf.Abs(cell.LatitudeMin - cell.LatitudeMax));
        var longitudeSegments = Mathf.CeilToInt(VerticesPerDegree * Mathf.Abs(cell.LongitudeMin - cell.LongitudeMax));
        MeshFilter.sharedMesh = MeshGenerator.GenerateSphereSectorRounded(cell.LatitudeMin, cell.LatitudeMax, cell.LongitudeMin, cell.LongitudeMax, 0.95f, latitudeSegments, longitudeSegments, 0.25f);
    }
}
