#if ( SERVER )
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DailyPuzzleGenManager : MonoBehaviour
{
    public static DailyPuzzleGenManager Instance;

    public PuzzlePackGeneratorLite Generator;

    public List<PackGenerationConfig> PuzzleGenParameters;
    // 0 means free, any non-zero value means the requester must specify that value for us to provide the details
    public List<int> AvailabilityKeys;

    public DailyPuzzles PuzzlesYesterday;
    public DailyPuzzles PuzzlesToday;
    public DailyPuzzles PuzzlesTomorrow;

    private DateTime Yesterday;
    private DateTime Today;
    private DateTime Tomorrow;

    void Start()
    {
        if (Instance)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Generator = new PuzzlePackGeneratorLite();

        Generator.FirstPuzzle = 1;
        Generator.LastPuzzle = 1;

        UpdateDate();
        GeneratePuzzles(dayPassMode: false);
    }

    private const float CHECK_DATE_INTERVAL = 60;
    private float TimeUntilCheckDate = CHECK_DATE_INTERVAL;
    void Update()
    {
        TimeUntilCheckDate -= Time.deltaTime;
        if(TimeUntilCheckDate <= 0)
        {
            bool dateChanged = UpdateDate();
            if(dateChanged)
            {
                GeneratePuzzles(dayPassMode: true);
            }
            TimeUntilCheckDate += CHECK_DATE_INTERVAL;
        }
    }

    private bool UpdateDate()
    {
        var today = DateTime.Now.Date;
        if(today > Today)
        {
            Yesterday = today.AddDays(-1).Date;
            Today = today;
            Tomorrow = today.AddDays(1).Date;
            return true;
        }
        return false;
    }

    private void GeneratePuzzles(bool dayPassMode)
    {
        // Optionally save some processing by reusing generated puzzles from days past
        if(dayPassMode)
        {
            PuzzlesYesterday = PuzzlesToday;
            PuzzlesToday = PuzzlesTomorrow;
        }

        var seed = GetSeed(Tomorrow);
        PuzzlesTomorrow = GeneratePuzzles(seed);
        if (!dayPassMode)
        {
            // Otherwise generate today's and yesterday's too
            seed = GetSeed(Today);
            PuzzlesToday = GeneratePuzzles(seed);
            seed = GetSeed(Yesterday);
            PuzzlesYesterday = GeneratePuzzles(seed);
        }
    }

    private int GetSeed(DateTime dateTime)
    {
        var date = dateTime.Date;
        return date.Year * 10000 + date.Month * 100 + date.Day;
    }

    private DailyPuzzles GeneratePuzzles(int seed)
    {
        var seeds = new List<int>();
        for(int i = 0; i < PuzzleGenParameters.Count; i++)
        {
            seeds.Add(seed);
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        var puzzleSet = new DailyPuzzles();
        int id = 1;
        foreach(var parameters in PuzzleGenParameters)
        {
            Generator.MasterSeed = seeds[id-1].ToString();
            Generator.Config = parameters;
            var puzzleConfig = Generator.GeneratePackPuzzles().First();
            puzzleConfig.Pack = "Daily";
            puzzleConfig.Id = id.ToString();
            puzzleSet.Puzzles.Add(puzzleConfig);
            id++;
        }
        foreach(var puzzle in puzzleSet.Puzzles)
        {
            var redacted = ScriptableObject.CreateInstance<PuzzleConfig>();
            redacted.Pack = puzzle.Pack;
            redacted.Id = puzzle.Id;
            puzzleSet.RedactedPuzzles.Add(redacted);
        }
        return puzzleSet;
    }

    public List<PuzzleConfig> GetDailyPuzzles(DateTime dateTime, ISet<int> availabilityKeys)
    {
        var results = new List<PuzzleConfig>();
        DailyPuzzles puzzleSet;
        if (dateTime.Date == Yesterday)
        {
            puzzleSet = PuzzlesYesterday;
        }
        else if (dateTime.Date == Today)
        {
            puzzleSet = PuzzlesToday;
        }
        else if (dateTime.Date == Tomorrow)
        {
            puzzleSet = PuzzlesTomorrow;
        }
        else
        {
            return results;
        }

        for(int i = 0; i < puzzleSet.Puzzles.Count; i++)
        {
            if(AvailabilityKeys[i] == 0 || availabilityKeys.Contains(AvailabilityKeys[i]))
            {
                results.Add(puzzleSet.Puzzles[i]);
            }
            else
            {
                results.Add(puzzleSet.RedactedPuzzles[i]);
            }
        }

        return results;
    }
}

[Serializable]
public class DailyPuzzles
{
    public List<PuzzleConfig> Puzzles = new List<PuzzleConfig>();
    public List<PuzzleConfig> RedactedPuzzles = new List<PuzzleConfig>();
}
#endif