using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/GriddedPuzzleConfig", order = 1)]
public class GriddedPuzzleConfig : ScriptableObject
{
	[Header("Grid")]
	public int[] GridCellsPerRow;

	[Header("Obstacles")]
	public Vector2Int[] RockPositions;
	public Vector2Int[] WallPositions;

	[Header("Nodes")]
	public Vector2Int[] NodePositions;
	public int[] NodeColors;
	public int[] SolutionLengths;
	public Vector2Int[] Solutions;
}
