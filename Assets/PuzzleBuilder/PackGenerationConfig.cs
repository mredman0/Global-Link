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
    [Header("Nodes")]
    [Range(2, 6)]
    public int MinNodePairs;
    [Range(2, 6)]
    public int MaxNodePairs;
    [Tooltip("Uses range [MinNodePairs, MaxNodePairs]")]
    public AnimationCurve TargetNodePairs;

    [Header("Waypoints")]
    [Tooltip("Based on number of node pairs")]
    public AnimationCurve TargetWaypoints;

    [Header("Warps")]
    [Range(0, 6)]
    public int MinWarpPairs;
    [Range(0, 6)]
    public int MaxWarpPairs;
    [Tooltip("Uses range [MinWarpPairs, MaxWarpPairs]")]
    public AnimationCurve TargetWarpPairs;

    [Header("Walls")]
    public AnimationCurve PreWalling;
    public AnimationCurve PreWallClustering;
    public AnimationCurve PostWalling;
}
