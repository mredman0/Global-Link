using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;

    [Header("Stats")]
    public int Sessions;
    public int PuzzlesSolved;
    public double GamePlaytime;
    public double PuzzlePlaytime;

    [Header("State")]
    public bool TrackPuzzlePlaytime;
    public DateTime PuzzlePlaytimeStart;
    public DateTime GamePlaytimeStart;

    // Start is called before the first frame update
    void Start()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadStats();
        Sessions++;
        SaveInt("StatsSessions", Sessions);
        AccumulateGameTime();
    }

    private void LoadStats()
    {
        Sessions = LoadInt("StatsSessions", 0);
        PuzzlesSolved = LoadInt("StatsPuzzlesSolved", 0);
        GamePlaytime = LoadDouble("StatsGamePlaytime", 0d);
        PuzzlePlaytime = LoadDouble("StatsPuzzlePlaytime", 0d);
    }

	#region Game Lifecycle
	private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            StoreGameTime();
            if(TrackPuzzlePlaytime)
            {
                StorePuzzleTime();
            }
        }
        else
        {
            AccumulateGameTime();
            if(TrackPuzzlePlaytime)
            {
                AccumulatePuzzleTime();
            }
        }
    }

    private void OnApplicationQuit()
    {
        StoreGameTime();
        if(TrackPuzzlePlaytime)
        {
            StorePuzzleTime();
        }
    }

    public void OnPuzzleStarted()
    {
        TrackPuzzlePlaytime = true;
        AccumulatePuzzleTime();
    }

    public void OnPuzzlePaused()
    {
        TrackPuzzlePlaytime = false;
        StorePuzzleTime();
    }

    public void OnPuzzleUnpaused()
    {
        TrackPuzzlePlaytime = true;
        AccumulatePuzzleTime();
    }

    public void OnPuzzleSolved()
    {
        PuzzlesSolved++;
        SaveInt("StatsPuzzlesSolved", PuzzlesSolved);

        TrackPuzzlePlaytime = false;
        StorePuzzleTime();
    }

    public void OnPuzzleClosed()
    {
        if(TrackPuzzlePlaytime)
        {
            TrackPuzzlePlaytime = false;
            StorePuzzleTime();
        }
    }
    #endregion

    public double LivePuzzlePlaytime
    {
        get
        {
            if(TrackPuzzlePlaytime)
            {
                return PuzzlePlaytime + (DateTime.Now - PuzzlePlaytimeStart).TotalSeconds;
            }
            return PuzzlePlaytime;
        }
    }

    private void AccumulateGameTime()
    {
        GamePlaytimeStart = DateTime.Now;
    }
    private void StoreGameTime()
    {
        GamePlaytime += (DateTime.Now - GamePlaytimeStart).TotalSeconds;
        SaveDouble("StatsGamePlaytime", GamePlaytime);
    }

    private void AccumulatePuzzleTime()
    {
        PuzzlePlaytimeStart = DateTime.Now;
    }
    private void StorePuzzleTime()
    {
        PuzzlePlaytime += (DateTime.Now - PuzzlePlaytimeStart).TotalSeconds;
        SaveDouble("StatsPuzzlePlaytime", PuzzlePlaytime);
    }

    private int LoadInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);
    private double LoadDouble(string key, double defaultValue = 0d)
    {
        var valStr = PlayerPrefs.GetString(key, "");
        if(double.TryParse(valStr, out double val))
        {
            return val;
        }
        return defaultValue;
    }
    private void SaveInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    private void SaveDouble(string key, double value) => PlayerPrefs.SetString(key, value.ToString());
}
