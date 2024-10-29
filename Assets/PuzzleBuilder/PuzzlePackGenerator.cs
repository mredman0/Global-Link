using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class PuzzlePackGenerator : MonoBehaviour
{
    [Header("Required References")]
    public PuzzleBuilder Builder;

    [Header("Settings")]
    public PackGenerationConfig Config;
    public string MasterSeed;
    public string PreviousSeed;
    public int Float01DiscreteBuckets;

    [Header("Overrides")]
    public int FirstPuzzle;
    public int LastPuzzle;

    [Header("Actions")]
    public bool Generate;

    private float[] NodePairProbabilities;
    private float[] WarpPairProbabilities;
    private float[] PreWallingProbabilities;
    private float[] PreWallClusteringProbabilities;
    private float[] PostWallingProbabilities;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        if(Generate)
        {
            Generate = false;
            GeneratePackPuzzles();
        }
    }

    private void GeneratePackPuzzles()
    {
        var valid = AssertValidConfig();
        if(!valid)
        {
            Debug.LogWarning($"No action taken, invalid config. See above errors");
            return;
        }

        var wouldOverwrite = WouldOverwritePuzzles();
        if(wouldOverwrite.Any())
        {
            var values = string.Join(',', wouldOverwrite);
            Debug.LogWarning($"Did not generate puzzles, as the following {Config.PackId} puzzles would have been overwritten: {values}");
            return;
        }

        var pack = Config.PackId;
        var min = Mathf.Max(FirstPuzzle, 1);
        var max = Config.NumPuzzles;
        if (LastPuzzle > 0)
        {
            max = LastPuzzle;
        }

        var masterSeedInt = CalculateMasterSeed();
        Random.InitState(masterSeedInt);

        var seeds = new int[max - min + 1];
        var seedsForPrinting = new SeedsForPrinting()
        {
            Seeds = new Vector2Int[seeds.Length]
        };
        for(int i = 0; i < seeds.Length; i++)
        {
            seeds[i] = Random.Range(int.MinValue, int.MaxValue);
            seedsForPrinting.Seeds[i] = new Vector2Int(i + min, seeds[i]);
        }

        var seedsTextPath = Path.Combine(Application.dataPath, $"Editor/Resources/Pack Generation/{pack}_seeds_{min}-{max}.txt");
        File.WriteAllText(seedsTextPath, JsonUtility.ToJson(seedsForPrinting, prettyPrint: true));

        NodePairProbabilities = DiscretizeCustomRangeCurve(Config.TargetNodePairs, Config.MinNodePairs, Config.MaxNodePairs);
        WarpPairProbabilities = DiscretizeCustomRangeCurve(Config.TargetWarpPairs, Config.MinWarpPairs, Config.MaxWarpPairs);
        PreWallingProbabilities = DiscretizeFloat01Curve(Config.PreWalling);
        PreWallClusteringProbabilities = DiscretizeFloat01Curve(Config.PreWallClustering);
        PostWallingProbabilities = DiscretizeFloat01Curve(Config.PostWalling);

        int seedToUse = 0;
        for (int i = min; i <= max; i++)
        {
            GeneratePuzzle(i, seeds[seedToUse]);
            seedToUse++;
        }
    }

    private bool AssertValidConfig()
    {
        var valid = true;
        if(!Config)
        {
            Debug.LogError($"No pack generation config provided");
            valid = false;
        }

        // Nodes
        if(Config.MinNodePairs > Config.MaxNodePairs)
        {
            Debug.LogError($"MinNodePairs is greater than MaxNodePairs");
            valid = false;
        }
        if(Config.TargetNodePairs.length < 1)
        {
            Debug.LogError($"TargetNodePairs has no keyframes");
            valid = false;
        }

        // Waypoints
        if (Config.TargetWaypoints.length < 1)
        {
            Debug.LogError($"TargetWaypoints has no keyframes");
            valid = false;
        }

        // Warps
        if (Config.MinWarpPairs > Config.MaxWarpPairs)
        {
            Debug.LogError($"MinWarpPairs is greater than MaxWarpPairs");
            valid = false;
        }
        if (Config.TargetWarpPairs.length < 1)
        {
            Debug.LogError($"TargetWarpPairs has no keyframes");
            valid = false;
        }

        // Walls
        if (Config.PreWalling.length < 1)
        {
            Debug.LogError($"PreWalling has no keyframes");
            valid = false;
        }
        if (Config.PreWallClustering.length < 1)
        {
            Debug.LogError($"PreWallClustering has no keyframes");
            valid = false;
        }
        if (Config.PostWalling.length < 1)
        {
            Debug.LogError($"PostWalling has no keyframes");
            valid = false;
        }

        return valid;
    }

    private List<int> WouldOverwritePuzzles()
    {
        var pack = Config.PackId;
        var min = Mathf.Max(FirstPuzzle, 0);
        var max = Config.NumPuzzles;
        if(LastPuzzle > 0)
        {
            max = LastPuzzle;
        }
        var wouldOverwrite = new List<int>();
        for(int i = min; i <= max; i++)
        {
            string resourcePath = $"Puzzles/{pack}/{pack}_{i}";
            if(Resources.Load<PuzzleConfig>(resourcePath))
            {
                wouldOverwrite.Add(i);
            }
        }
        return wouldOverwrite;
    }

    private int CalculateMasterSeed()
    {
        int seed = string.IsNullOrEmpty(MasterSeed) ? (int)DateTime.Now.Ticks : MasterSeed.GetHashCode();
        if (int.TryParse(MasterSeed, out int seedNumber))
        {
            seed = seedNumber;
        }
        PreviousSeed = string.IsNullOrEmpty(MasterSeed) ? seed.ToString() : MasterSeed;
        return seed;
    }


    private void GeneratePuzzle(int puzzleIdInPack, int seed)
    {
        Builder.Clear();
        Builder.RebuildGrid();

        var nodePairs = RandomIntFromCurve(NodePairProbabilities, Config.MinNodePairs);
        
        var waypointProbabilities = DiscretizeCustomRangeCurve(Config.TargetWaypoints, 0, nodePairs);
        var waypoints = RandomIntFromCurve(waypointProbabilities, 0);

        var warps = RandomIntFromCurve(WarpPairProbabilities, Config.MinWarpPairs);

        var preWalling = RandomFloat01FromCurve(PreWallingProbabilities);
        var preWallClustering = RandomFloat01FromCurve(PreWallClusteringProbabilities);
        var postWalling = RandomFloat01FromCurve(PostWallingProbabilities);

        Builder.GeneratorSeed = seed.ToString();

        Builder.TargetNodePairs = nodePairs;
        Builder.TargetWaypoints = waypoints;
        Builder.TargetWarpPairs = warps;
        Builder.InitialWallAmount = preWalling;
        Builder.WallClustering = preWallClustering;
        Builder.AdditionalWallAmount = postWalling;

        Builder.PuzzleName = $"{Config.PackId}_{puzzleIdInPack}";
        Builder.GeneratePuzzle();
        Builder.Save();
    }

    private int RandomIntFromCurve(float[] curve, int min)
    {
        var rand = Random.value;
        for(int i = 0; i < curve.Length; i++)
        {
            if(rand < curve[i])
            {
                return min + i;
            }
            rand -= curve[i];
        }
        return min + curve.Length - 1;
    }
    private float RandomFloat01FromCurve(float[] curve)
    {
        var rand = Random.value;
        for(int i = 0; i < curve.Length; i++)
        {
            if(rand < curve[i])
            {
                return (float)i / curve.Length;
            }
            rand -= curve[i];
        }
        return 1;
    }

    private float[] DiscretizeCustomRangeCurve(AnimationCurve curve, int min, int max) => DiscretizeProbabilityCurve(curve, max - min + 1);
    private float[] DiscretizeFloat01Curve(AnimationCurve curve) => DiscretizeProbabilityCurve(curve, Float01DiscreteBuckets);
    private float[] DiscretizeProbabilityCurve(AnimationCurve curve, int buckets)
    {
        if(buckets <= 1)
        {
            return new float[1] { 1 };
        }

        var result = new float[buckets];

        // Get raw values
        for(int i = 0; i < buckets; i++)
        {
            float t = (float)i / (buckets - 1);
            result[i] = curve.Evaluate(t);
        }

        // Normalize
        var total = result.Sum();
        for(int i = 0; i < buckets; i++)
        {
            result[i] /= total;
        }

        return result;
    }

    [Serializable]
    private class SeedsForPrinting
    {
        public Vector2Int[] Seeds;
    }
}
