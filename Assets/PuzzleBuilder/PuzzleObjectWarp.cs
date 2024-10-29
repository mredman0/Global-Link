using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleObjectWarp : PuzzleObject
{
    public GameObject WarpLinePrefab;

    public GridCell Cell;
    public PuzzleObjectWarp PairedWarp;

    public LineRenderer LineToPairedWarp;

    // Start is called before the first frame update
    void Start()
    {

    }

    public void SetPairedWarp(PuzzleObjectWarp other)
    {
        Unpair(issueWarning: false);
        PairedWarp = other;

        var linePoints = new Vector3[2];
        linePoints[0] = Cell.transform.position;
        linePoints[1] = PairedWarp.Cell.transform.position;
        var lineGO = Instantiate(WarpLinePrefab);
        var line = lineGO.GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPositions(linePoints);
        LineToPairedWarp = line;

        // Meet your new neighbor!
        Cell.Neighbors.Add(PairedWarp.Cell);
    }

    public void Unpair(bool issueWarning = false)
    {
        if (PairedWarp)
        {
            if(issueWarning)
            {
                Debug.LogWarning($"Warp in {PairedWarp.Cell.name} no longer linked (previously linked to {Cell.name})");
            }

            Cell.Neighbors.Remove(PairedWarp.Cell);
            PairedWarp.Cell.Neighbors.Remove(Cell);

            if (LineToPairedWarp)
            {
                Destroy(LineToPairedWarp.gameObject);
            }
            if (PairedWarp.LineToPairedWarp)
            {
                Destroy(PairedWarp.LineToPairedWarp.gameObject);
            }
            PairedWarp.PairedWarp = null;
            PairedWarp = null;
        }
    }
}
