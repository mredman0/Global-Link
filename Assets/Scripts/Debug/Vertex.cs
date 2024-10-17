using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertex : MonoBehaviour
{
    public const int NUM_ADJACENT_VERTICES = 4;

    public List<Vertex> Adjacent = new List<Vertex>();

    // Start is called before the first frame update
    void Start()
    {

    }

    public float GetDistance(Vertex other) => Vector3.Distance(transform.position, other.transform.position);

	#region Adjacency
	public void DetermineAdjacency(List<Vertex> allVertices)
    {
        var distanceToBeat = float.MaxValue;
        var adjacentDistances = new List<float>();

        for (int i = 0; i < allVertices.Count; i++)
        {
            if (allVertices[i] == this)
            {
                continue;
            }
            var distance = Vector3.Distance(allVertices[i].transform.position, transform.position);
            if (distance < distanceToBeat)
            {
                InsertInOrder(adjacentDistances, allVertices[i], distance);
                if (Adjacent.Count > NUM_ADJACENT_VERTICES)
                {
                    Adjacent.RemoveAt(NUM_ADJACENT_VERTICES);
                    adjacentDistances.RemoveAt(NUM_ADJACENT_VERTICES);
                }
                if(Adjacent.Count >= NUM_ADJACENT_VERTICES)
                {
                    distanceToBeat = adjacentDistances[NUM_ADJACENT_VERTICES - 1];
                }
            }
        }
    }

    private void InsertInOrder(List<float> adjacentDistances, Vertex v, float distance)
    {
        for (int i = 0; i < Adjacent.Count; i++)
        {
            if (distance < adjacentDistances[i])
            {
                Adjacent.Insert(i, v);
                adjacentDistances.Insert(i, distance);
                return;
            }
        }
        Adjacent.Add(v);
        adjacentDistances.Add(distance);
    }

    public void EnsureCommutativeAdjacency()
    {
        foreach (var v in Adjacent)
        {
            if (!v.Adjacent.Contains(this))
            {
                v.Adjacent.Add(this);
            }
        }
    }

    public bool AssertCommutativeAdjacency()
    {
        foreach(var v in Adjacent)
        {
            if(!v.Adjacent.Contains(this))
            {
                return false;
            }
        }
        return true;
    }
	#endregion
}
