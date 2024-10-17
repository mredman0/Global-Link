using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    [Header("Required References")]
    public MeshFilter MeshFilter;
    public MeshRenderer MeshRenderer;

    [Header("Settings")]
    public int VertexDensity = 10;

    public GridCell GridCell { get; set; }

    // Start is called before the first frame update
    void Start()
    {

    }

    public void SetGridCell(GridCell cell)
    {
        GridCell = cell;
        MeshFilter.sharedMesh = MeshGenerator.GenerateSphereSector(cell.LatitudeMin, cell.LatitudeMax, cell.LongitudeMin, cell.LongitudeMax, 1f, VertexDensity, VertexDensity);
    }
}
