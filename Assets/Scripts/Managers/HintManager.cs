using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    public static HintManager Instance;

    public event Action HintGained;
    public event Action HintUsed;

    private const string HINTS_KEY = "Hints";
    private const int DEFAULT_HINTS = 3;

    private int Hints;

    // Start is called before the first frame update
    void Start()
    {
        if(Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadHints();
    }

    public bool UseHint()
    {
        if(Hints < 1)
        {
            return false;
        }
        Hints--;
        SaveHints();
        HintUsed?.Invoke();
        return true;
    }

    public void GainHints(int amount)
    {
        if(amount < 1)
        {
            Debug.LogWarning("Do not use GainHints to reduce number of hints.");
            return;
        }
        Hints += amount;
        SaveHints();
        HintGained?.Invoke();
    }

    public int GetHints() => Hints;

    private void SaveHints()
    {
        PlayerPrefs.SetInt(HINTS_KEY, Hints);
    }

    private void LoadHints()
    {
        Hints = PlayerPrefs.GetInt(HINTS_KEY, DEFAULT_HINTS);
    }
}
