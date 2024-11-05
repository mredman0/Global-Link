using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainMenuBackground : MonoBehaviour
{
    [Header("Required References")]
    public GameObject RotatorPrefab;
    public GameObject LinePrefab;
    public GameObject NodePrefab;

    [Header("Settings")]
    [Range(1, 12)]
    public int NumArcs;

    public float DegreesPerPoint = 5f;
    
    [Range(20f, 270f)]
    public float MinArcDegrees = 20f;
    [Range(20f, 270f)]
    public float MaxArcDegrees = 180f;

    [Range(0.1f, 1f)]
    public float MinRadius = 0.1f;
    [Range(0.1f, 1f)]
    public float MaxRadius = 1f;

    public float MinRotationSpeed = 1f;
    public float MaxRotationSpeed = 5f;

    public bool AllowReverseRotation = true;

    // Start is called before the first frame update
    void Start()
    {
        var colors = new List<int>();
        void AddNewRandomColors()
        {
            var colorsToAdd = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                colorsToAdd.Add(i);
            }
            for (int i = 0; i < 6; i++)
            {
                var randInd = Random.Range(0, colorsToAdd.Count);
                colors.Add(colorsToAdd[randInd]);
                colorsToAdd.RemoveAt(randInd);
            }
        }
        AddNewRandomColors();

        var radiusStep = (MaxRadius - MinRadius) / (NumArcs - 1);

        var previousColor = -1;
        for (int i = 0; i < NumArcs; i++)
        {
            if(!colors.Any())
            {
                AddNewRandomColors();
                if(colors.First() == previousColor)
                {
                    var temp = colors[0];
                    colors[0] = colors[5];
                    colors[5] = temp;
                }
            }
            var color = ColorManager.Instance.ApplyActiveColorMap(colors.First());
            previousColor = colors.First();
            colors.RemoveAt(0);

            var lineRotatorGO = Instantiate(RotatorPrefab, transform);
            var node1RotatorGO = Instantiate(RotatorPrefab, transform);
            var node2RotatorGO = Instantiate(RotatorPrefab, transform);

            var speed = Random.Range(MinRotationSpeed, MaxRotationSpeed);
            if(AllowReverseRotation && Random.value < 0.5f)
            {
                speed *= -1f;
            }
            var lineRotator = lineRotatorGO.GetComponent<MainMenuBackgroundRotator>();
            lineRotator.Speed = speed;
            var node1Rotator = node1RotatorGO.GetComponent<MainMenuBackgroundRotator>();
            node1Rotator.Speed = speed;
            var node2Rotator = node2RotatorGO.GetComponent<MainMenuBackgroundRotator>();
            node2Rotator.Speed = speed;

            var newLine = Instantiate(LinePrefab, lineRotatorGO.transform).GetComponent<LineRenderer>();
            var newNode1 = Instantiate(NodePrefab, node1RotatorGO.transform);
            var newNode2 = Instantiate(NodePrefab, node2RotatorGO.transform);

            var radius = MinRadius + (radiusStep * i);

            var startLat = Random.Range(0f, 360f);
            var endLat = startLat + Random.Range(MinArcDegrees, MaxArcDegrees);

            var numPoints = Mathf.CeilToInt((endLat - startLat) / DegreesPerPoint);
            var step = (endLat - startLat) / (numPoints - 1);

            var startPolar = new PolarVector3(radius, startLat, 0f);
            var endPolar = new PolarVector3(radius, endLat, 0f);

            newNode1.transform.localPosition = startPolar.ToCartesian();
            newNode1.GetComponent<Renderer>().material.SetColor("_Color", color);
            newNode2.transform.localPosition = endPolar.ToCartesian();
            newNode2.GetComponent<Renderer>().material.SetColor("_Color", color);

            newLine.material.SetColor("_Color", color);
            newLine.positionCount = numPoints;
            for(int j = 0; j < numPoints; j++)
            {
                var deg = startPolar.Latitude + (step * j);
                var polar = new PolarVector3(radius, deg, 0f);
                newLine.SetPosition(j, polar.ToCartesian());
            }
        }
    }
}
