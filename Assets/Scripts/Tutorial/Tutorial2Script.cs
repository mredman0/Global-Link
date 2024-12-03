using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutorial2Script : MonoBehaviour
{
    public int ColorToConnectNaively;

    // Start is called before the first frame update
    void Start()
    {
        var puzzle = Puzzle.Current;

        if (!puzzle)
        {
            Debug.LogError($"Tutorial2Script could not find puzzle to modify");
            return;
        }

        var nodes = puzzle.NodesByColor[ColorToConnectNaively];
        var cellStart = nodes[0].GridCell;
        var cellEnd = nodes[1].GridCell;

        puzzle.SetUndoState(ColorToConnectNaively);

        var pathToDraw = puzzle.Grid.GetShortestPath(cellStart, cellEnd);
        puzzle.DrawPathForColor(ColorToConnectNaively, pathToDraw);
    }
}
