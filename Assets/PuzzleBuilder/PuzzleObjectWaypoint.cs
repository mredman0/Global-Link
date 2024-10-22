using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleObjectWaypoint : PuzzleObject
{
    public GridCell Cell;

    public int Color;

    // Start is called before the first frame update
    void Start()
    {

    }

    public void SetColor(int color)
    {
        Color = color;
    }
}
