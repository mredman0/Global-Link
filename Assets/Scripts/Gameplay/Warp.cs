using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Warp : MonoBehaviour
{
    [Header("Required References")]
    public MeshFilter MeshFilter;
    public MeshRenderer MeshRenderer;
    public Animator Animator;
    public GameObject WarpPreviewLinePrefab;

    [Header("Settings")]
    public int Color;
    public float VerticesPerDegree = 0.5f;
    public Warp PairedWarp;

    [Header("State")]
    public WarpRole Role = WarpRole.Open;
    public LineRenderer WarpPreviewLine;

    public GridCell GridCell { get; set; }

    private Vector3? PointDrawnInCell;

    // Start is called before the first frame update
    void Start()
    {

    }

    private void SetColor(int colorIndex)
    {
        Color = colorIndex;
        //Animator.SetInteger("ColorIndex", colorIndex);
    }

    public void LinePointDrawnInCell(LineRenderer path, Vector3 point, int color)
    {
        if(Role == WarpRole.Open)
        {
            PointDrawnInCell = point;
            Role = WarpRole.Source;
            PairedWarp.Role = WarpRole.Destination;
            SetColor(color);
            ApplyWarpToPath(path);
        }
    }

    public void TakeWarp(LineRenderer path, Vector3 point, int color, bool applyWarpToPath)
    {
        PointDrawnInCell = point;
        Role = WarpRole.Source;
        PairedWarp.Role = WarpRole.Destination;
        PairedWarp.SetColor(color);
        SetColor(color);
        if(applyWarpToPath)
        {
            ApplyWarpToPath(path);
        }
        WarpPreviewLine.gameObject.SetActive(false);
    }

    private void ApplyWarpToPath(LineRenderer path)
    {
        path.positionCount += 4;
        path.SetPosition(path.positionCount - 4, GridCell.transform.position);
        path.SetPosition(path.positionCount - 3, GridCell.transform.position * 0.89f);
        path.SetPosition(path.positionCount - 2, PairedWarp.GridCell.transform.position * 0.89f);
        path.SetPosition(path.positionCount - 1, PairedWarp.GridCell.transform.position);
    }

    public bool LinePointRemovedFromCell(Vector3 point)
    {
        if(PointDrawnInCell == point)
        {
            if(Role == WarpRole.Source)
            {
                Role = WarpRole.Open;
                PairedWarp.Role = WarpRole.Open;
            }
            PointDrawnInCell = null;
            SetColor(-1);
            PairedWarp.SetColor(-1);
            WarpPreviewLine.gameObject.SetActive(true);
            return true;
        }
        return false;
    }

    public void SetGridCell(GridCell cell)
    {
        GridCell = cell;
        var latitudeSegments = Mathf.CeilToInt(VerticesPerDegree * Mathf.Abs(cell.LatitudeMin - cell.LatitudeMax));
        var longitudeSegments = Mathf.CeilToInt(VerticesPerDegree * Mathf.Abs(cell.LongitudeMin - cell.LongitudeMax));
        MeshFilter.sharedMesh = MeshGenerator.GenerateSphereSectorRounded(cell.LatitudeMin, cell.LatitudeMax, cell.LongitudeMin, cell.LongitudeMax, 0.95f, latitudeSegments, longitudeSegments, 0.25f);
    }

    public void SetPairedWarp(Warp other)
    {
        PairedWarp = other;
        other.PairedWarp = this;

        var previewLineGO = Instantiate(WarpPreviewLinePrefab, transform.parent);
        WarpPreviewLine = previewLineGO.GetComponent<LineRenderer>();
        PairedWarp.WarpPreviewLine = WarpPreviewLine;

        var linePoints = new Vector3[2];
        linePoints[0] = GridCell.transform.position * 0.9f;
        linePoints[1] = PairedWarp.GridCell.transform.position * 0.9f;
        WarpPreviewLine.positionCount = 2;
        WarpPreviewLine.SetPositions(linePoints);
    }

    public enum WarpRole
    {
        Open,
        Source,
        Destination
    }
}
