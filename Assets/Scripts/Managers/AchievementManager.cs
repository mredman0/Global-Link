#if UNITY_ANDROID
using GooglePlayGames;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    private Dictionary<Achievement, string> AchievementIds = new Dictionary<Achievement, string>();

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

        if(PlayerAuthManager.Instance.IsAuthenticated)
        {
            OnPlayerAuthenticated();
        }
        PlayerAuthManager.Instance.AuthenticationComplete += OnPlayerAuthenticated;
    }

    private void OnDestroy()
    {
        PlayerAuthManager.Instance.AuthenticationComplete -= OnPlayerAuthenticated;
    }

    private void OnPlayerAuthenticated()
    {
        AchievementIds.Clear();
#if UNITY_ANDROID
        AchievementIds.Add(Achievement.PacksBeginner, "CgkIqpOH0vcZEAIQBg");
        AchievementIds.Add(Achievement.PacksIntermediate, "CgkIqpOH0vcZEAIQBw");
        AchievementIds.Add(Achievement.PacksExpert, "CgkIqpOH0vcZEAIQCA");
        AchievementIds.Add(Achievement.PacksGrandmaster, "CgkIqpOH0vcZEAIQCQ");

        AchievementIds.Add(Achievement.DailyNovice, "CgkIqpOH0vcZEAIQAw");
        AchievementIds.Add(Achievement.DailyAdept, "CgkIqpOH0vcZEAIQBA");
        AchievementIds.Add(Achievement.DailyGuru, "CgkIqpOH0vcZEAIQBQ");

        AchievementIds.Add(Achievement.ManyTunnels, "CgkIqpOH0vcZEAIQCg");
        AchievementIds.Add(Achievement.MultiTunnelLine, "CgkIqpOH0vcZEAIQCw");
        AchievementIds.Add(Achievement.TunnelAvoider, "CgkIqpOH0vcZEAIQDA");
#endif
    }

    public void SetCompleted(Achievement ach) => SetProgress(ach, 100.0);

    public void SetProgress(Achievement ach, double percentDone)
    {
#if UNITY_EDITOR
        return;
#endif
        if(!AchievementIds.ContainsKey(ach))
        {
            return;
        }
        var id = AchievementIds[ach];

#if UNITY_ANDROID
        PlayGamesPlatform.Instance.ReportProgress(id, percentDone, (success) =>
        {
            if(success)
            {
                Debug.Log($"Achievement {id}: progress of {percentDone} reported");
            }
            else
            {
                Debug.LogError($"Achievement {id}: progress of {percentDone} failed to be reported");
            }
        });
#endif
    }

    public void Increment(Achievement ach, int steps = 1)
    {
#if UNITY_EDITOR
        return;
#endif
        if (!AchievementIds.ContainsKey(ach))
        {
            return;
        }
        var id = AchievementIds[ach];

#if UNITY_ANDROID
        PlayGamesPlatform.Instance.IncrementAchievement(id, steps, (success) =>
        {
            if (success)
            {
                Debug.Log($"Achievement {id}: incremented by {steps}");
            }
            else
            {
                Debug.LogError($"Achievement {id}: failed to be incremented by {steps}");
            }
        });
#endif
    }

    public enum Achievement
    {
        PacksBeginner,
        PacksIntermediate,
        PacksExpert,
        PacksGrandmaster,
        DailyNovice,
        DailyAdept,
        DailyGuru,
        ManyTunnels,
        MultiTunnelLine,
        TunnelAvoider
    }
}
