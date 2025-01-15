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
    private Dictionary<string, Achievement> AchievementsById = new Dictionary<string, Achievement>();
    private Dictionary<Achievement, double> AchievementProgress = new Dictionary<Achievement, double>();

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
#if !UNITY_EDITOR
        DefineAchievement(Achievement.PacksBeginner, "CgkIqpOH0vcZEAIQBg");
        DefineAchievement(Achievement.PacksIntermediate, "CgkIqpOH0vcZEAIQBw");
        DefineAchievement(Achievement.PacksExpert, "CgkIqpOH0vcZEAIQCA");
        DefineAchievement(Achievement.PacksGrandmaster, "CgkIqpOH0vcZEAIQCQ");

        DefineAchievement(Achievement.DailyNovice, "CgkIqpOH0vcZEAIQAw");
        DefineAchievement(Achievement.DailyAdept, "CgkIqpOH0vcZEAIQBA");
        DefineAchievement(Achievement.DailyGuru, "CgkIqpOH0vcZEAIQBQ");

        DefineAchievement(Achievement.ManyTunnels, "CgkIqpOH0vcZEAIQCg");
        DefineAchievement(Achievement.MultiTunnelLine, "CgkIqpOH0vcZEAIQCw");
        DefineAchievement(Achievement.TunnelAvoider, "CgkIqpOH0vcZEAIQDA");
#endif
#if !UNITY_EDITOR && UNITY_IOS
        Social.LoadAchievements((achievements) =>
        {
            foreach(var achievement in achievements)
            {
                AchievementProgress.Add(AchievementsById[achievement.id], achievement.percentCompleted);
            }
        });
#endif
    }

    private void DefineAchievement(Achievement ach, string id)
    {
        AchievementIds.Add(ach, id);
        AchievementsById.Add(id, ach);
    }

    public void SetCompleted(Achievement ach) => SetProgress(ach, 100.0);

    public void SetProgress(Achievement ach, double percentDone)
    {
        if(!AchievementIds.ContainsKey(ach))
        {
            return;
        }
        var id = AchievementIds[ach];

#if UNITY_ANDROID || UNITY_IOS
        Social.ReportProgress(id, percentDone, (success) =>
        {
            if (success)
            {
                Debug.Log($"Achievement {id}: progress of {percentDone} reported");
            }
            else
            {
                Debug.LogError($"Achievement {id}: progress of {percentDone} failed to be reported");
            }
        });
        if(!AchievementProgress.ContainsKey(ach))
        {
            AchievementProgress.Add(ach, percentDone);
        }
        else
        {
            AchievementProgress[ach] = percentDone;
        }
#endif
    }

    public void Increment(Achievement ach, int steps = 1)
    {
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
#elif UNITY_IOS
        if (!AchievementIncrementSteps.ContainsKey(ach))
        {
            Debug.LogError($"Cannot increment achievement {ach} progress, it is not defined as incrementable");
            return;
        }
        var incrementProgress = ((double)steps / AchievementIncrementSteps[ach]) * 100;
        var currentProgress = AchievementProgress.ContainsKey(ach) ? AchievementProgress[ach] : 0;
        Social.ReportProgress(AchievementIds[ach], currentProgress + incrementProgress, (success) =>
        {
            if (success)
            {
                if (!AchievementProgress.ContainsKey(ach))
                {
                    AchievementProgress.Add(ach, currentProgress + incrementProgress);
                }
                else
                {
                    AchievementProgress[ach] = currentProgress + incrementProgress;
                }
                Debug.Log($"Achievement {id}: incremented by {steps}");
            }
            else
            {
                Debug.LogError($"Achievement {id}: failed to be incremented by {steps}");
            }
        });
#endif
    }

    private Dictionary<Achievement, int> AchievementIncrementSteps = new Dictionary<Achievement, int>()
    {
        { Achievement.DailyNovice, 10 },
        { Achievement.DailyAdept, 100 },
        { Achievement.DailyGuru, 1000 },
    };

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
