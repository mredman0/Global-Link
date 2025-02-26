using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReviewRequestManager : MonoBehaviour
{
    public static ReviewRequestManager Instance;

    [Header("Settings")]
    public int FirstAskSessions = 3;
    public int FirstAskPuzzlesSolved = 15;
    public double FirstAskPuzzlePlaytime = 60 * 30; // 30 minutes
    public int SubsequentAskSessions = 2;
    public int SubsequentAskPuzzlesSolved = 40;
    public int SubsequentAskPuzzlePlaytime = 60 * 90; // 90 minutes

    [Header("State")]
    public bool Ask;
    public int AskAfterSession;
    public int AskAfterPuzzlesSolved;
    public double AskAfterPuzzlePlaytime;

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
        LoadParameters();
    }

    public void OnPuzzleSolved()
    {
        if(ConditionsMet)
        {
            AskPlayer();
        }
    }

    public void SendToStoreForReview()
    {
#if UNITY_EDITOR
        Application.OpenURL("https://play.google.com/store/apps/details?id=com.redprismgames.chromasphere&hl=en-US");
#elif UNITY_ANDROID
        Application.OpenURL("market://details?id=com.redprismgames.chromasphere");
#elif UNITY_IOS
        Application.OpenURL("itms-apps://itunes.apple.com/app/id6740209857");
#endif
        StopAsking();
    }

    public void StopAsking()
    {
        Ask = false;
        PlayerPrefs.SetInt("RR_Ask", 0);
    }

    private void AskPlayer()
    {
        // Immediately increment conditions so we could NEVER double-show
        AskAfterSession += SubsequentAskSessions;
        AskAfterPuzzlesSolved += SubsequentAskPuzzlesSolved;
        AskAfterPuzzlePlaytime += SubsequentAskPuzzlePlaytime;

        PlayerPrefs.SetInt("RR_AskAfterSession", AskAfterSession);
        PlayerPrefs.SetInt("RR_AskAfterPuzzlesSolved", AskAfterPuzzlesSolved);
        SaveDouble("RR_AskAfterPuzzlePlaytime", AskAfterPuzzlePlaytime);

        FindFirstObjectByType<RequestReviewDialog>(FindObjectsInactive.Include).Show();
    }

    private bool ConditionsMet => Ask &&
        StatsManager.Instance.Sessions >= AskAfterSession &&
        StatsManager.Instance.PuzzlesSolved >= AskAfterPuzzlesSolved &&
        StatsManager.Instance.LivePuzzlePlaytime >= AskAfterPuzzlePlaytime;

    private void LoadParameters()
    {
        Ask = PlayerPrefs.GetInt("RR_Ask", 1) != 0;
        AskAfterSession = PlayerPrefs.GetInt("RR_AskAfterSession", FirstAskSessions);
        AskAfterPuzzlesSolved = PlayerPrefs.GetInt("RR_AskAfterPuzzlesSolved", FirstAskPuzzlesSolved);
        AskAfterPuzzlePlaytime = LoadDouble("RR_AskAfterPuzzlePlaytime", FirstAskPuzzlePlaytime);
    }

    private double LoadDouble(string key, double defaultValue = 0d)
    {
        var valStr = PlayerPrefs.GetString(key, "");
        if (double.TryParse(valStr, out double val))
        {
            return val;
        }
        return defaultValue;
    }
    private void SaveDouble(string key, double value) => PlayerPrefs.SetString(key, value.ToString());
}
