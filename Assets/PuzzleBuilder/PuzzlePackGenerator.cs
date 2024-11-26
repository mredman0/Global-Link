#if ( UNITY_EDITOR )
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
    public int MaxAttemptsPerPuzzle = 15;

    [Header("Overrides")]
    public int FirstPuzzle;
    public int LastPuzzle;

    [Header("Actions")]
    public bool Generate;

    private float[] NodePairProbabilities;
    private float[] WarpPairProbabilities;
    private float[] PreWallingProbabilities;
    private float[] PreWallClusteringProbabilities;
    private float[] PreWallNoodlingProbabilities;
    private float[] PostWallingProbabilities;

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

        NodePairProbabilities = DiscretizeCustomRangeCurve(Config.TargetNodePairs, Config.MinNodePairs, Config.MaxNodePairs);
        WarpPairProbabilities = DiscretizeCustomRangeCurve(Config.TargetWarpPairs, Config.MinWarpPairs, Config.MaxWarpPairs);
        PreWallingProbabilities = DiscretizeFloat01Curve(Config.PreWalling);
        PreWallClusteringProbabilities = DiscretizeFloat01Curve(Config.PreWallClustering);
        PreWallNoodlingProbabilities = DiscretizeFloat01Curve(Config.PreWallNoodling);
        PostWallingProbabilities = DiscretizeFloat01Curve(Config.PostWalling);

        var parameters = new PackGenerationParameters()
        {
            Id = pack,
            MasterSeed = masterSeedInt,
            PuzzleParameters = new List<PuzzleGenerationParameters>()
        };
        for(int i = min; i <= max; i++)
        {
            parameters.PuzzleParameters.Add(GeneratePuzzleParameters(i, Random.Range(int.MinValue, int.MaxValue)));
        }

        var complexities = parameters.PuzzleParameters.Select(p => p.Complexity());
        parameters.MinComplexity = complexities.Min();
        parameters.MaxComplexity = complexities.Max();
        parameters.MeanComplexity = complexities.Average();
        var sortedComplexities = complexities.OrderBy(c => c).ToList();
        int count = sortedComplexities.Count;
        if (count % 2 == 0)
        {
            // Even number of elements
            parameters.MedianComplexity = (sortedComplexities[count / 2 - 1] + sortedComplexities[count / 2]) / 2f;
        }
        else
        {
            // Odd number of elements
            parameters.MedianComplexity = sortedComplexities[count / 2];
        }
        parameters.ComplexityStDev = Mathf.Sqrt(complexities.Sum(c => Mathf.Pow(c - parameters.MeanComplexity, 2)) / count);

        var failedEnforcement = 0;
        for (int i = min; i <= max; i++)
        {
            int attemptsTaken = GeneratePuzzleWithEnforcement(parameters.PuzzleParameters[i-min]);
            if(attemptsTaken > MaxAttemptsPerPuzzle)
            {
                failedEnforcement++;
            }
        }

        if(failedEnforcement > 0)
        {
            Debug.LogError($"{failedEnforcement} puzzles could not meet enforcement parameters after {MaxAttemptsPerPuzzle} attempts");
        }
        else
        {
            Debug.Log($"All puzzles generated meeting enforcement parameters");
        }

        var seedsTextPath = Path.Combine(Application.dataPath, $"Editor/Resources/Pack Generation/{pack}_params_{min}-{max}.txt");
        File.WriteAllText(seedsTextPath, JsonUtility.ToJson(parameters, prettyPrint: true));
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
        if (Config.TargetColorsPlusWaypoints < Config.MinNodePairs)
        {
            Debug.LogError($"TargetColorsPlusWaypoints is less than MinNodePairs");
            valid = false;
        }
        if (Config.TargetColorsPlusWaypoints < Config.MaxNodePairs)
        {
            Debug.LogWarning($"TargetColorsPlusWaypoints is less than MaxNodePairs");
        }
        if (Config.TargetColorsPlusWaypoints > Config.MinNodePairs*2)
        {
            Debug.LogError($"TargetColorsPlusWaypoints is greater than MinNodePairs*2");
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
        if (Config.PreWallNoodling.length < 1)
        {
            Debug.LogError($"PreWallNoodling has no keyframes");
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
            string resourcePath = $"{pack}/{i}.asset";
            if(GameManager.AssetExists<PuzzleConfig>(resourcePath))
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

    private PuzzleGenerationParameters GeneratePuzzleParameters(int puzzleNum, int seed)
    {
        var parameters = new PuzzleGenerationParameters()
        {
            Id = puzzleNum,
            Seed = seed
        };

        var nodePairs = RandomIntFromCurve(NodePairProbabilities, Config.MinNodePairs);

        var waypoints = Mathf.Clamp(Config.TargetColorsPlusWaypoints - nodePairs, 0, nodePairs);

        var warps = RandomIntFromCurve(WarpPairProbabilities, Config.MinWarpPairs);

        var preWalling = RandomFloat01FromCurve(PreWallingProbabilities);
        var preWallClustering = RandomFloat01FromCurve(PreWallClusteringProbabilities);
        var preWallNoodling = RandomFloat01FromCurve(PreWallNoodlingProbabilities);
        var postWalling = RandomFloat01FromCurve(PostWallingProbabilities);

        parameters.NodePairs = nodePairs;
        parameters.Waypoints = waypoints;
        parameters.WarpPairs = warps;
        parameters.InitialWalling = preWalling;
        parameters.WallClustering = preWallClustering;
        parameters.WallNoodling = preWallNoodling;
        parameters.AdditionalWalling = postWalling;

        return parameters;
    }

    private int GeneratePuzzleWithEnforcement(PuzzleGenerationParameters parameters)
    {
        int attempt = 0;
        void Generate(int seed)
        {
            //Debug.Log($"Generating puzzle {parameters.Id} (attempt {attempt}) with complexity {parameters.Complexity()}");

            Builder.Clear();
            Builder.RebuildGrid();

            Builder.GeneratorSeed = seed.ToString();

            Builder.TargetNodePairs = parameters.NodePairs;
            Builder.TargetWaypoints = parameters.Waypoints;
            Builder.TargetWarpPairs = parameters.WarpPairs;
            Builder.InitialWallAmount = parameters.InitialWalling;
            Builder.InitialWallClustering = parameters.WallClustering;
            Builder.InitialWallNoodling = parameters.WallNoodling;
            Builder.AdditionalWallAmount = parameters.AdditionalWalling;

            Builder.Pack = Config.PackId;
            Builder.Id = $"{parameters.Id}";
            Builder.GeneratePuzzle();
        }
        bool PassesEnforcement()
        {
            if(((float)Builder.Nodes.Count / 2f) / Builder.TargetNodePairs < Config.EnforceMinColors)
            {
                return false;
            }
            if(((float)Builder.Warps.Count / 2f) / Builder.TargetWarpPairs < Config.EnforceMinWarpPairs)
            {
                return false;
            }
            if (((float)Builder.Waypoints.Count) / Builder.TargetWaypoints < Config.EnforceMinWaypoints)
            {
                return false;
            }
            return true;
        }

        var seed = parameters.Seed;
        Random.InitState(parameters.Seed);
        while (attempt < MaxAttemptsPerPuzzle)
        {
            attempt++;
            seed = Random.Range(int.MinValue, int.MaxValue);
            Generate(seed);
            if(PassesEnforcement())
            {
                break;
            }
        }

        if (!PassesEnforcement())
        {
            attempt++; // Signal that the result does not meet enforcement after all attempts
        }
        parameters.Seed = seed;
        Builder.Save();
        return attempt;
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
    private class PackGenerationParameters
    {
        public string Id;
        public int MasterSeed;
        public float MinComplexity;
        public float MaxComplexity;
        public float MeanComplexity;
        public float ComplexityStDev;
        public float MedianComplexity;
        public List<PuzzleGenerationParameters> PuzzleParameters;
    }

    [Serializable]
    private class PuzzleGenerationParameters
    {
        public int Id;
        public int Seed;

        public int NodePairs;
        public int Waypoints;
        public int WarpPairs;
        public float InitialWalling;
        public float WallClustering;
        public float WallNoodling;
        public float AdditionalWalling;

        public float Complexity()
        {
            var complexity = 0f;

            complexity += NodePairs;
            complexity += Waypoints;
            complexity += WarpPairs;

            // InitialWalling should make things generally less complex due to creating less space to work with for everything else
            // So, shorter paths in general
            // The "less complexity" feels even more so if the walls are in a giant cluster
            var initialWallComplexity = InitialWalling * (1f + WallClustering) * -2f;
            complexity += initialWallComplexity;

            complexity *= (1f + AdditionalWalling);

            return complexity;
        }
    }
}
#endif