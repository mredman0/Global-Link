using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node : MonoBehaviour
{
    public Puzzle Puzzle;
    public int Color;
    public bool Connected = false;
    public bool Active = false;

    public GameObject PathPrefab;
    public List<GridCell> GridPath = new List<GridCell>();
    public LineRenderer Path;

    public GridCell GridCell;

    public Node PairedNode { get; private set; }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseUp()
    {
        if(Puzzle)
        {
            Puzzle.NodeOnMouseUp(this);
        }
    }

    public void Activate()
    {
        Active = true;

        GridPath.Clear();
        GridPath.Add(GridCell);
        if(Path)
        {
            Destroy(Path.gameObject);
        }
        Path = Instantiate(PathPrefab).GetComponent<LineRenderer>();

        var pathColor = ColorMapController.Instance.ApplyActiveColorMap(Color);
        Path.material.SetColor("_Color", pathColor);
        if(Puzzle)
        {
            Path.transform.parent = Puzzle.transform;
        }

        Path.positionCount++;
        Path.SetPosition(0, transform.position);
    }

    public void Deactivate()
    {
        Active = false;
    }

    public void SetColor(int color)
    {
        Color = color;
        var mappedColor = ColorMapController.Instance.ApplyActiveColorMap(color);
        GetComponent<Renderer>().material.SetColor("_Color", mappedColor);
        if(Path)
        {
            Path.material.SetColor("_Color", mappedColor);
        }
    }

    public void SetPairedNode(Node other)
    {
        PairedNode = other;
    }


    private const float MINIMUM_POINT_ADD_DISTANCE_SQ = 0.0005f;
    private const float LOOP_MERGE_DISTANCE_SQ = 0.0005f;
    public void Draw(Vector3 point)
    {
        int mergeLoop;
        for (mergeLoop = 0; mergeLoop < Path.positionCount-1; mergeLoop++)
        {
            if (Vector3.SqrMagnitude(point - Path.GetPosition(mergeLoop)) < LOOP_MERGE_DISTANCE_SQ)
            {
                break;
            }
        }
        if(mergeLoop < Path.positionCount - 2)
        {
            Path.positionCount = mergeLoop+1;
        }

        if(Vector3.SqrMagnitude(point - Path.GetPosition(Path.positionCount - 1)) > MINIMUM_POINT_ADD_DISTANCE_SQ)
        {
            Path.positionCount++;
            Path.SetPosition(Path.positionCount - 1, point);
        }
    }

    private const float LINE_BETWEEN_CELLS_STEP_SIZE = 1f;
    public bool AddCellToPath(GridCell cell)
    {
        if(GridPath.Contains(cell))
        {
            if(GridPath.Last() != cell)
            {
                // Cut loop
                var index = GridPath.IndexOf(cell);
                CutCellFromPath(GridPath[index + 1]);
            }
        }
        else
        {
            if(!GridPath.Last().Neighbors.Contains(cell))
            {
                return false;
            }

            cell.Color = Color;
            GridPath.Add(cell);

            var startPos = Path.GetPosition(Path.positionCount - 1).ToPolar();
            var endPos = cell.transform.position.ToPolar();

            var startLong = startPos.Longitude;
            var endLong = endPos.Longitude;

            if(startPos.Latitude == 90f || startPos.Latitude == -90f)
            {
                startLong = endLong;
            }
            else if(endPos.Latitude == 90f || endPos.Latitude == -90f)
            {
                endLong = startLong;
            }

            var newPoints = new List<Vector3>();
            var latDiff = endPos.Latitude - startPos.Latitude;
            var longDiff = endLong - startLong;
            if(longDiff > 180f)
            {
                longDiff -= 360f;
            }
            else if(longDiff < -180f)
            {
                longDiff += 360f;
            }
            var step = new Vector2(latDiff, longDiff).normalized * LINE_BETWEEN_CELLS_STEP_SIZE;
            var numSteps = Mathf.FloorToInt(new Vector2(latDiff, longDiff).magnitude / step.magnitude) - 1;
            var latStep = step.x;
            var longStep = step.y;
            for(int i = 1; i < numSteps; i++)
            {
                var currentLat = startPos.Latitude + latStep * i;
                var currentLong = startLong + longStep * i;
                newPoints.Add(PolarVector3.ToCartesian(currentLat, currentLong));
            }
            newPoints.Add(cell.transform.position);

            var existingPositions = new Vector3[Path.positionCount];
            Path.GetPositions(existingPositions);
            Path.positionCount += newPoints.Count;
            Path.SetPositions(existingPositions.Concat(newPoints).ToArray());
        }

        return true;
    }

    public void CutCellFromPath(GridCell cell)
    {
        var index = GridPath.IndexOf(cell);
        if(index == 0)
        {
            // Cannot cut off the start of the path from the path
            return;
        }
        var endOfPathCell = GridPath[index - 1];
        for (int i = index; i < GridPath.Count; i++)
        {
            GridPath[i].Color = null;
        }
        GridPath = GridPath.Take(index).ToList();

        var linePositions = new Vector3[Path.positionCount];
        Path.GetPositions(linePositions);
        for (index = 0; index < linePositions.Length; index++)
        {
            if (linePositions[index] == endOfPathCell.transform.position)
            {
                break;
            }
        }
        Path.positionCount = index + 1;
        Path.SetPositions(linePositions.Take(index + 1).ToArray());
    }

    public void AutoConnectToPairedNode()
    {
        //var pathPositions = UnitSphereUtil.FindPathOnUnitSphere(transform.position, PairedNode.transform.position, p => Puzzle.IsPositionFree(p, Color));

        //Path = Instantiate(PathPrefab).GetComponent<LineRenderer>();
        //var pathColor = ColorMapController.Instance.ApplyActiveColorMap(Color);
        //Path.material.SetColor("_Color", pathColor);
        //Path.transform.parent = Puzzle.transform;

        //Path.positionCount = pathPositions.Count;
        //Path.SetPositions(pathPositions.ToArray());
    }
}
