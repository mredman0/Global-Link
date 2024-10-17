using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSphereUtil : MonoBehaviour
{
    public GameObject PathPrefab;
    public GameObject NodePrefab;
    public GameObject VertexPrefab;


    public int NumVertices = 5000;
    public float VertexSize = 0.15f;
    private List<Vertex> Vertices = new List<Vertex>();

    // Start is called before the first frame update
    void Start()
    {
        CreateVertices();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CreateVertices()
    {
        var points = GetVertices(NumVertices);
        var pointNum = 0;
        foreach (var point in points)
        {
            var vert = Instantiate(VertexPrefab);
            vert.name = $"Vertex {pointNum}";
            vert.transform.position = point;
            vert.transform.localScale = new Vector3(VertexSize, VertexSize, VertexSize);
            Vertices.Add(vert.GetComponent<Vertex>());
            pointNum++;
        }

        var startTime = Time.realtimeSinceStartup;
        foreach (var vertex in Vertices)
        {
            vertex.DetermineAdjacency(Vertices);
        }
        var endTime = Time.realtimeSinceStartup;
        Debug.Log($"Determining adjacency for {NumVertices} vertices took {endTime - startTime} seconds");

        int verticesWithNonCommutativeAdjacency = 0;
        foreach (var vertex in Vertices)
        {
            vertex.EnsureCommutativeAdjacency();
            if (!vertex.AssertCommutativeAdjacency())
            {
                verticesWithNonCommutativeAdjacency++;
            }
        }
        if (verticesWithNonCommutativeAdjacency > 0)
        {
            Debug.LogWarning($"{verticesWithNonCommutativeAdjacency} vertices have non-commutative adjacency, this could be a problem!");
        }
        else
        {
            Debug.Log($"No vertices have non-commutative adjacency, all good!");
        }
    }

    public static Vector3[] GetVertices(int n)
    {
        if (n <= 2 || n % 2 != 0)
        {
            Debug.LogError("n must be a positive even integer greater than 2.");
            return null;
        }

        Vector3[] vertices = new Vector3[n];
        var phi = Mathf.PI * (Mathf.Sqrt(5f) - 1);

        for(int i = 0; i < n; i++)
        {
            var y = 1 - (i / (float)(n - 1)) * 2;
            var radius = Mathf.Sqrt(1 - y * y);

            var theta = phi * i;

            var x = Mathf.Cos(theta) * radius;
            var z = Mathf.Sin(theta) * radius;

            vertices[i] = new Vector3(x, y, z);
        }

        return vertices;
    }
}
