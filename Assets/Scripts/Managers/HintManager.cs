using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    public static HintManager Instance;

    public event Action Initialized;

    public event Action HintGained;
    public event Action HintUsed;

    private const string HINTS_KEY = "Hints";
    private const string OFFLINE_USED_HINTS_KEY = "Hints_OU";
    private const string DEFAULT_HINTS_GRANTED_KEY = "Hints_DG";
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

        if(PlayerAuthManager.Instance.IsAuthenticated)
        {
            StartupSync();
        }
        PlayerAuthManager.Instance.AuthenticationComplete += StartupSync;

        PurchaseManager.Instance.HintsPurchased += GainHintsSync;
    }

    private void OnDestroy()
    {
        PlayerAuthManager.Instance.AuthenticationComplete -= StartupSync;
        PurchaseManager.Instance.HintsPurchased -= GainHintsSync;
    }

    private void StartupSync()
    {
        _ = Startup();
    }
    private async Task Startup()
    {
        var loaded = await LoadHints();
        if(!loaded)
        {
            var defaultGranted = PlayerPrefs.GetInt(DEFAULT_HINTS_GRANTED_KEY, 0);
            if(defaultGranted == 0)
            {
                Hints = DEFAULT_HINTS;
                var saved = await SaveHints();
                if(saved)
                {
                    PlayerPrefs.SetInt(DEFAULT_HINTS_GRANTED_KEY, 1);
                }
            }
        }
        var offlineUsed = PlayerPrefs.GetInt(OFFLINE_USED_HINTS_KEY, 0);
        if (offlineUsed > 0)
        {
            var previous = Hints;
            Hints -= Mathf.Max(0, Hints - offlineUsed);
            var saved = await SaveHints();
            if(!saved)
            {
                Hints = previous;
            }
            else
            {
                PlayerPrefs.DeleteKey(OFFLINE_USED_HINTS_KEY);
            }
        }
        Initialized?.Invoke();
    }

    public bool UseHint()
    {
#if !DEMO
        if(Hints < 1)
        {
            return false;
        }
        Hints--;
        SaveHints().ContinueWith((t =>
        {
            if(!t.Result)
            {
                PlayerPrefs.SetInt(OFFLINE_USED_HINTS_KEY, PlayerPrefs.GetInt(OFFLINE_USED_HINTS_KEY, 0) + 1);
            }
        }));
#endif
        HintUsed?.Invoke();
        return true;
    }

    private void GainHintsSync(int amount)
    {
        _ = GainHints(amount);
    }
    public async Task GainHints(int amount)
    {
        if(amount < 1)
        {
            Debug.LogWarning("Do not use GainHints to reduce number of hints.");
            return;
        }
#if !DEMO
        Hints += amount;
        await SaveHints();
#endif
        HintGained?.Invoke();
    }

    public int GetHints() => Hints;

    private async Task<bool> SaveHints()
    {
        try
        {
            var data = new Dictionary<string, object>() { { HINTS_KEY, Hints } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> LoadHints()
    {
        try
        {
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>() { HINTS_KEY });
            Hints = data[HINTS_KEY].Value.GetAs<int>();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
