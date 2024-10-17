using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PuzzleConfig", order = 1)]
public class PuzzleConfig : ScriptableObject
{
	public int NumNodes;
	public Vector3[] NodePositions;
	public int[] NodeColors;
}
