#if ( UNITY_EDITOR )
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PackGenerationConfig", order = 2)]
public class PackGenerationConfig : ScriptableObject
{
    [Header("Pack Info")]
    public string PackId;
    public int NumPuzzles;

    [Header("Puzzle Generation Parameters")]
    [Header("Mode")]
    public PuzzleGenerationMode Mode;

    [Header("Nodes")]
    [Range(2, 6)]
    public int MinNodePairs;
    [Range(2, 6)]
    public int MaxNodePairs;
    [Tooltip("Uses range [MinNodePairs, MaxNodePairs]")]
    public AnimationCurve TargetNodePairs;

    [Header("Waypoints")]
    [Range(2, 12)]
    public int TargetColorsPlusWaypoints;

    [Header("Warps")]
    [Range(0, 6)]
    public int MinWarpPairs;
    [Range(0, 6)]
    public int MaxWarpPairs;
    [Tooltip("Uses range [MinWarpPairs, MaxWarpPairs]")]
    public AnimationCurve TargetWarpPairs;

    [Header("Initial Walls")]
    public AnimationCurve PreWalling;
    public float PreWallNormalness = 1;
    public AnimationCurve PreWallClustering;
    public AnimationCurve PreWallNoodling;

    [Header("Additional Walls")]
    public AnimationCurve PostWalling;

    [Header("Enforcement")]
    [Range(0f, 1f)]
    public float EnforceMinColors = 0f;
    [Range(0f, 1f)]
    public float EnforceMinWarpPairs = 0f;
    [Range(0f, 1f)]
    public float EnforceMinWaypoints = 0f;
}

#endif