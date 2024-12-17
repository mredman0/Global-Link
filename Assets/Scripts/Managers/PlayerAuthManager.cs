using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Services.Authentication;
using UnityEngine;

public class PlayerAuthManager : MonoBehaviour
{
    public static PlayerAuthManager Instance;

    public event Action AuthenticationComplete;
    public event Action AuthenticationFailed;

    private SynchronizationContext MainThreadContext;

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
        MainThreadContext = SynchronizationContext.Current;

#if DEMO
        return;
#endif

#if UNITY_EDITOR
        Action onStartup = null;
#elif (UNITY_ANDROID)
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();
        Action onStartup = LoginGooglePlayGames;
#else
        Action onStartup = null;
#endif
        if (onStartup is null)
        {
            return;
        }
        if (UnityServicesManager.Instance.Initialized)
        {
            onStartup();
        }
        UnityServicesManager.Instance.ServicesInitialized += onStartup;
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        Action onStartup = null;
#elif (UNITY_ANDROID)
        Action onStartup = LoginGooglePlayGames;
#else
        Action onStartup = null;
#endif
        if(onStartup is null)
        {
            return;
        }
        UnityServicesManager.Instance.ServicesInitialized -= onStartup;
    }

#if (UNITY_ANDROID)
    public void LoginGooglePlayGames()
    {
        if (SynchronizationContext.Current != MainThreadContext)
        {
            MainThreadContext.Post(_ => LoginGooglePlayGames(), null);
            return;
        }

        if (PlayGamesPlatform.Instance.localUser.authenticated)
        {
            Debug.Log($"Google Play Games user already authenticated... {PlayGamesPlatform.Instance.localUser.id}");
            PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
            {
                Debug.Log("Authorization code: " + code);
                if(!string.IsNullOrEmpty(code))
                {
                    SignInWithGooglePlayGames(code);
                }
            });
            return;
        }

        Debug.Log("Beginning Google Play Games authentication...");
        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if (success == SignInStatus.Success)
            {
                Debug.Log("Login with Google Play games successful.");
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    Debug.Log("Authorization code: " + code);
                    if (!string.IsNullOrEmpty(code))
                    {
                        SignInWithGooglePlayGames(code);
                    }
                });
            }
            else
            {
                Debug.LogError("Failed to retrieve Google play games authorization code");
            }
        });
    }

    private void SignInWithGooglePlayGames(string authCode)
    {
        AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode).ContinueWith(task => {
            if (task.IsCompleted)
            {
                Debug.Log("Successfully signed in with Google Play Games.");
                AuthenticationComplete?.Invoke();
            }
            else
            {
                Debug.LogError("Google Play Games sign-in failed.");
                AuthenticationFailed?.Invoke();
            }
        });
    }
#endif
}
