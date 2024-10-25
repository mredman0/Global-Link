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

    public void Activate(bool newPath)
    {
        Active = true;

        if(newPath)
        {
            Path = Instantiate(PathPrefab).GetComponent<LineRenderer>();

            var pathColor = ColorMapController.Instance.ApplyActiveColorMap(Color);
            Path.material.SetColor("_Color", pathColor);
            if (Puzzle)
            {
                Path.transform.parent = Puzzle.transform;
            }

            Path.positionCount++;
            Path.SetPosition(0, transform.position);
        }
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
}
