using com.unity3d.mediation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    public event Action SdkLoadFailed;

    public event Action<bool> AdFreeChanged;

    public event Action BannerAdInitialized;

    public event Action InterstitialLoaded;
    public event Action InterstitialLoadFailed;
    public event Action InterstitialClosed;

    public event Action RewardedHintLoaded;
    public event Action RewardedHintLoadFailed;
    public event Action RewardedHintClosed;

    public event Action<string, int> AdRewarded;

    [Header("GLOBAL DO ADS")]
    public bool GLOBAL_DO_ADS = true;
    public bool PROCEED_WITHOUT_STORE = false;

    [Header("App Keys")]
    public string AndroidAppKey;
    public string iOSAppKey;

    [Header("Ad Unit Ids")]
    public string AndroidBannerAdUnitId;
    public string iOSBannerAdUnitId;
    public string AndroidInterstitialAdUnitId;
    public string iOSInterstitialAdUnitId;
    public string AndroidRewardedHintAdUnitId;
    public string iOSRewardedHintAdUnitId;

    [Header("Placement Ids")]
    public string AndroidRewardedHintPlacementId;
    public string iOSRewardedHintPlacementId;

    [Header("Interstitial Settings")]
    public uint PuzzlesPerInterstitial = 2;
    public float RetryLoadInterstitialDelay;

    [Header("State")]
    public bool RewardedHintAvailable;
    public int BannerAdHeight = 0;
    public bool AdFreeMode;

    [Header("Debug")]
    public bool TestSuiteMode;
    public bool DoValidation;
    public bool AllowAdsInDevelopmentBuild;
    public bool ForceBannerSpace;
    public int DpiOverride = -1;

    private bool AllAdsDisabled = false;
    private string AppKey = null;
    private string BannerAdUnitId;
    private string InterstitialAdUnitId;
    private string RewardedHintAdUnitId;

    private string RewardedHintPlacementId;

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

#if DEMO
        SetAdFree(true);
#endif

        if(PROCEED_WITHOUT_STORE)
        {
            OnPurchaseManagerInitialized();
        }
        else
        {
            if (PurchaseManager.Instance.IsInitialized)
            {
                OnPurchaseManagerInitialized();
            }
            PurchaseManager.Instance.Initialized += OnPurchaseManagerInitialized;
        }
        PurchaseManager.Instance.AdFreeChanged += SetAdFree;
    }

    private void OnDestroy()
    {
        PurchaseManager.Instance.Initialized -= OnPurchaseManagerInitialized;
        PurchaseManager.Instance.AdFreeChanged -= SetAdFree;

        BannerAd?.DestroyAd();
        InterstitialAd?.DestroyAd();
        RewardedHintAd?.DestroyAd();
    }

    private void OnPurchaseManagerInitialized()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        AllAdsDisabled = !GLOBAL_DO_ADS || !AllowAdsInDevelopmentBuild;
#else
        AllAdsDisabled = !GLOBAL_DO_ADS;
#endif

#if UNITY_IOS
        AppKey = iOSAppKey;
        BannerAdUnitId = iOSBannerAdUnitId;
        InterstitialAdUnitId = iOSInterstitialAdUnitId;
        RewardedHintAdUnitId = iOSRewardedHintAdUnitId;
        RewardedHintPlacementId = iOSRewardedHintPlacementId;
#elif UNITY_ANDROID
        AppKey = AndroidAppKey;
        BannerAdUnitId = AndroidBannerAdUnitId;
        InterstitialAdUnitId = AndroidInterstitialAdUnitId;
        RewardedHintAdUnitId = AndroidRewardedHintAdUnitId;
        RewardedHintPlacementId = AndroidRewardedHintPlacementId;
#endif

        void SetBannerHeight()
        {
#if UNITY_EDITOR
            float density = DpiOverride > 0 ? DpiOverride / 160f : Screen.dpi / 160f;
#else
            float density = Screen.dpi / 160f;
#endif
            float projectedBannerHeight = 50f * density; // actual height, may need conversion to canvas reference height
#if UNITY_IOS
            // On iOS, if there is no bottom safe area, assume a hardware nav button which the ads SDK might give a 20px? buffer to prevent accidental clicks
            // MAYBE ALSO DO THIS ON ANDROID
            if(Screen.safeArea.y < 1f)
            {
                projectedBannerHeight += 20;
            }
#endif
            // Round up to next multiple of 10
            projectedBannerHeight /= 10f;
            projectedBannerHeight = Mathf.Ceil(projectedBannerHeight);
            projectedBannerHeight *= 10f;

            BannerAdHeight = (int)projectedBannerHeight;
        }

        if (AllAdsDisabled || string.IsNullOrWhiteSpace(AppKey))
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(ForceBannerSpace)
            {
                SetBannerHeight();
            }
#endif
            return;
        }

            if (TestSuiteMode)
        {
            IronSource.Agent.setMetaData("is_test_suite", "enable");
        }

        SetBannerHeight();

        LevelPlay.OnInitFailed += OnSdkInitFailed;
        LevelPlay.OnInitSuccess += OnSdkInitSuccess;
#if UNITY_ANDROID
        var userId = GooglePlayGames.PlayGamesPlatform.Instance?.GetUserId() ?? "";
#elif UNITY_IOS
        var userId = Apple.GameKit.GKLocalPlayer.Local?.GamePlayerId ?? "";
#endif
        Debug.Log($"Initializing LevelPlay with userId: {userId}");
        LevelPlay.Init(AppKey, userId);
    }

    private void OnApplicationPause(bool pause)
    {
        LevelPlay.SetPauseGame(pause);
    }

    private void OnSdkInitFailed(LevelPlayInitError error)
    {
        if (DoValidation)
        {
            IronSource.Agent.validateIntegration();
        }
        Debug.LogError("Ads SDK initialization failed, see following error for error code and message");
        Debug.LogError($"{error.ErrorCode}\n{error.ErrorMessage}");

        SdkLoadFailed?.Invoke();
    }
    private void OnSdkInitSuccess(LevelPlayConfiguration config)
    {
        if(DoValidation)
        {
            IronSource.Agent.validateIntegration();
        }
        if (TestSuiteMode)
        {
            //Launch test suite
            IronSource.Agent.launchTestSuite();
            return;
        }

        if(!AdFreeMode)
        {
            InitBannerAd();
            LoadInterstitial();
        }
        PuzzlesOpenedSinceLastInterstitial = 0; // Should this start higher to prevent abusing restarting the app to avoid interstitials?

        LoadRewardedHint();
    }


#region Banner
    private LevelPlayBannerAd BannerAd;
	private void InitBannerAd()
    {
        BannerAd = new LevelPlayBannerAd(BannerAdUnitId, LevelPlayAdSize.BANNER, LevelPlayBannerPosition.BottomCenter, respectSafeArea: false);
        BannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
        BannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
        BannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;
        BannerAd.OnAdDisplayFailed += BannerOnAdDisplayFailedEvent;
        BannerAd.OnAdClicked += BannerOnAdClickedEvent;
        BannerAd.OnAdCollapsed += BannerOnAdCollapsedEvent;
        BannerAd.OnAdLeftApplication += BannerOnAdLeftApplicationEvent;
        BannerAd.OnAdExpanded += BannerOnAdExpandedEvent;

        BannerAd.LoadAd();
        BannerAdInitialized?.Invoke();
    }
    void BannerOnAdLoadedEvent(LevelPlayAdInfo adInfo) { }
    void BannerOnAdLoadFailedEvent(LevelPlayAdError ironSourceError)
    {
        Debug.LogWarning("BannerOnAdLoadFailedEvent");
        Debug.LogWarning($"{ironSourceError.ErrorCode}: {ironSourceError.ErrorMessage}");
    }
    void BannerOnAdClickedEvent(LevelPlayAdInfo adInfo) { Debug.Log("BannerOnAdClickedEvent"); }
    void BannerOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
    void BannerOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError adInfoError)
    {
        Debug.LogWarning("BannerOnAdDisplayFailedEvent");
        Debug.LogWarning($"{adInfoError.LevelPlayError.ErrorCode}: {adInfoError.LevelPlayError.ErrorMessage}");
    }
    void BannerOnAdCollapsedEvent(LevelPlayAdInfo adInfo) { Debug.Log("BannerOnAdCollapsedEvent"); }
    void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo adInfo) { Debug.Log("BannerOnAdLeftApplicationEvent"); }
    void BannerOnAdExpandedEvent(LevelPlayAdInfo adInfo) { Debug.Log("BannerOnAdExpandedEvent"); }
#endregion

#region Interstitial
    // Interstitial Ads being shown depends on how many puzzles have been opened
    private uint PuzzlesOpenedSinceLastInterstitial;
    public void PuzzleOpened()
    {
        PuzzlesOpenedSinceLastInterstitial++;
    }
    private bool OpenedEnoughPuzzlesForInterstitial() => PuzzlesOpenedSinceLastInterstitial >= PuzzlesPerInterstitial;

    private LevelPlayInterstitialAd InterstitialAd;
    public bool LoadInterstitial()
    {
        if(AdFreeMode)
        {
            return false;
        }
        if(AllAdsDisabled)
        {
            Debug.Log("Not loading Interstitial Ad: all ads are disabled");
            return false;
        }
        if(InterstitialAd != null)
        {
            Debug.Log("Not loading Interstitial Ad: ad already loaded and not yet destroyed");
            return false;
        }
        if (LevelPlayInterstitialAd.IsPlacementCapped("Level_Complete"))
        {
            Debug.Log($"Not loading Interstitial Ad: placement capped, will check again in {RetryLoadInterstitialDelay} seconds");
            StartCoroutine(RetryLoadInterstitialAfterDelay());
            return false;
        }

        InterstitialAd = new LevelPlayInterstitialAd(InterstitialAdUnitId);
        InterstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
        InterstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
        InterstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
        InterstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
        InterstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
        InterstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
        InterstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;

        Debug.Log("Loading Interstitial Ad...");
        InterstitialAd.LoadAd();
        return true;
    }

    public bool ShowInterstitial()
    {
        if (AdFreeMode)
        {
            return false;
        }
        if (AllAdsDisabled)
        {
            Debug.Log("Interstitial Ad not shown: all ads are disabled");
            return false;
        }
        if (!OpenedEnoughPuzzlesForInterstitial())
        {
            Debug.Log("Interstitial Ad not shown: not enough puzzles opened since previous Interstitial Ad");
            return false;
        }
        if(InterstitialAd is null || !InterstitialAd.IsAdReady())
        {
            Debug.Log("Interstitial Ad not shown: Ad not ready");
            return false;
        }
        if(LevelPlayInterstitialAd.IsPlacementCapped("Level_Complete"))
        {
            Debug.Log("Interstitial Ad not shown: placement capped");
            return false;
        }
        PuzzlesOpenedSinceLastInterstitial = 0;
        Debug.Log("Showing Interstitial Ad");
        InterstitialAd.ShowAd("Level_Complete");
        return true;
    }

    void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad Loaded");
        InterstitialLoaded?.Invoke();
    }

    void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error)
    {
        Debug.Log($"Interstitial Ad Load failed: {error.ErrorCode} {error.ErrorMessage}");
        InterstitialAd?.DestroyAd();
        InterstitialAd = null;
        InterstitialLoadFailed?.Invoke();
    }

    void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void InterstitialOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError infoError)
    {
        InterstitialAd?.DestroyAd();
        InterstitialAd = null;
        InterstitialClosed?.Invoke();
    }

    void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
        InterstitialAd?.DestroyAd();
        InterstitialAd = null;
        InterstitialClosed?.Invoke();
    }

    void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
    {
    }

    private IEnumerator RetryLoadInterstitialAfterDelay()
    {
        yield return new WaitForSeconds(RetryLoadInterstitialDelay);
        Debug.Log($"Retrying to load Interstitial Ad after {RetryLoadInterstitialDelay} second delay");
        LoadInterstitial();
    }
#endregion

#region Rewarded Hint
    private LevelPlayRewardedAd RewardedHintAd;
    public bool LoadRewardedHint()
    {
        if (AllAdsDisabled)
        {
            Debug.Log("Not loading Rewarded Hint Ad: all ads are disabled");
            return false;
        }
        if (RewardedHintAd != null)
        {
            return false;
        }

        RewardedHintAd = new LevelPlayRewardedAd(RewardedHintAdUnitId);
        RewardedHintAd.OnAdLoaded += RewardedHintOnAdLoadedEvent;
        RewardedHintAd.OnAdLoadFailed += RewardedHintOnAdLoadFailedEvent;
        RewardedHintAd.OnAdDisplayed += RewardedHintOnAdDisplayedEvent;
        RewardedHintAd.OnAdDisplayFailed += RewardedHintOnAdDisplayFailedEvent;
        RewardedHintAd.OnAdClicked += RewardedHintOnAdClickedEvent;
        RewardedHintAd.OnAdClosed += RewardedHintOnAdClosedEvent;
        RewardedHintAd.OnAdInfoChanged += RewardedHintOnAdInfoChangedEvent;
        RewardedHintAd.OnAdRewarded += RewardedHintOnAdRewarded;

        RewardedHintAd.LoadAd();
        return true;
    }
    public bool ShowRewardedHint()
    {
        if (AllAdsDisabled)
        {
            Debug.Log("Not showing Rewarded Hint Ad: all ads are disabled");
            return false;
        }
        if (!RewardedHintAd.IsAdReady())
        {
            return false;
        }
        RewardedHintAd.ShowAd(RewardedHintPlacementId);
        return true;
    }

    void RewardedHintOnAdLoadedEvent(LevelPlayAdInfo adInfo)
    {
        RewardedHintAvailable = true;
        RewardedHintLoaded?.Invoke();
    }

    void RewardedHintOnAdLoadFailedEvent(LevelPlayAdError error)
    {
        RewardedHintAvailable = false;
        RewardedHintLoadFailed?.Invoke();
    }

    void RewardedHintOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void RewardedHintOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError infoError)
    {
        RewardedHintAvailable = false;
        RewardedHintAd?.DestroyAd();
        RewardedHintAd = null;
        RewardedHintClosed?.Invoke();
    }

    void RewardedHintOnAdClickedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void RewardedHintOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
        RewardedHintAvailable = false;
        if(RewardedHintAd != null)
        {
            RewardedHintAd.DestroyAd();
            RewardedHintAd = null;
            RewardedHintClosed?.Invoke();
        }
    }

    void RewardedHintOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void RewardedHintOnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"SANITY CHECK!!! reward name:{reward.Name}, reward amount:{reward.Amount}");

        // TEMPORARY iOS BUG WORKAROUND
#if UNITY_IOS
        Debug.Log("iOS ad reward bug fix... forcing reward amount to be 1...");
        AdRewarded?.Invoke(reward.Name, 1);
#else
        AdRewarded?.Invoke(reward.Name, reward.Amount);
#endif

        RewardedHintAvailable = false;
        if (RewardedHintAd != null)
        {
            RewardedHintAd.DestroyAd();
            RewardedHintAd = null;
            RewardedHintClosed?.Invoke();
        }
    }
#endregion

#region Ad-Free Mode
    public void SetAdFree(bool adFree)
    {
        AdFreeMode = adFree;
        if(adFree)
        {
            if (InterstitialAd != null)
            {
                InterstitialAd.DestroyAd();
                InterstitialAd = null;
            }
            if (BannerAd != null)
            {
                BannerAd.DestroyAd();
                BannerAd = null;
                BannerAdHeight = 0;
            }
        }
        else
        {
            if(InterstitialAd is null)
            {
                LoadInterstitial();
            }
            if(BannerAd is null)
            {
                InitBannerAd();
            }
        }
        AdFreeChanged?.Invoke(adFree);
    }
#endregion
}
