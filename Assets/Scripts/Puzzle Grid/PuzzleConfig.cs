using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/GriddedPuzzleConfig", order = 1)]
public class PuzzleConfig : ScriptableObject
{
	[Header("Metadata")]
	// Contains information about both what pack it belongs in and what number it is in that pack
	public string ID;

	[Header("Grid")]
	public int[] GridCellsPerRow;

	[Header("Obstacles")]
	public Vector2Int[] RockPositions;
	public Vector2Int[] WallPositions;

	[Header("Nodes")]
	public Vector2Int[] NodePositions;
	public int[] NodeColors;

	[Header("Waypoints")]
	public Vector2Int[] WaypointPositions;

	[Header("Solutions")]
	public int[] SolutionLengths;
	public Vector2Int[] Solutions;

	[Header("View")]
	public bool OpaqueSphere;
	public Quaternion CameraArmStart;
	public float CameraDistance;
	public float CameraFoV;
}
