using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Services.Authentication;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class PlayerAuthManager : MonoBehaviour
{
    public static PlayerAuthManager Instance;

    public event Action AuthenticationComplete;
    public event Action AuthenticationFailed;

    [Header("Required References")]
    public Canvas NotificationsDialogMainCanvas;
    public GameObject NotificationsDialogPrefab;

    [Header("Settings")]
    public bool DebugTestDSANotification;

    private SynchronizationContext MainThreadContext;
    private List<Notification> UnreadNotifications;

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
        _ = SignInWithGooglePlayGamesAsync(authCode);
    }

    private async Task SignInWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
            
            // Verify the LastNotificationDate
            var lastNotificationDate = AuthenticationService.Instance.LastNotificationDate;
            long storedNotificationDate = GetLastNotificationReadDate();
            // Verify if the LastNotification date is available and greater than the last read notifications
            if (lastNotificationDate != null && long.Parse(lastNotificationDate) > storedNotificationDate)
            {
                // Retrieve the notifications from the backend
                UnreadNotifications = await AuthenticationService.Instance.GetNotificationsAsync();
            }

            Debug.Log("Successfully signed in with Google Play Games.");
            AuthenticationComplete?.Invoke();
        }
        catch (AuthenticationException e)
        {
            // Read notifications from the banned player exception
            UnreadNotifications = e.Notifications;
            // Notify the player with the proper error message
            Debug.LogException(e);
            AuthenticationFailed?.Invoke();
        }
        catch (Exception e)
        {
            // Notify the player with the proper error message
            Debug.LogException(e);
            AuthenticationFailed?.Invoke();
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if(DebugTestDSANotification)
        {
            UnreadNotifications ??= new List<Notification>();
            UnreadNotifications.Add(new Notification()
            {
                CreatedAt = "1",
                CaseId = "TEST CASE ID",
                ProjectId = "TEST PROJECT ID",
                Message = "TEST MESSAGE, THIS IS A TEST NOTIFICATION FOR DSA COMPLIANCE. BELOW YOU WILL FIND SOME RANDOM CONTENT TO MAKE SURE IT IS FORMATTED REASONABLY.\nThis is a new paragraph. If you are reading this, this is a development build, and this message does not imply any required action for your account. If you would like more details, that's a shame because there aren't any."
            });
        }
#endif

        if(UnreadNotifications != null && UnreadNotifications.Count > 0)
        {
            var dialogGO = Instantiate(NotificationsDialogPrefab, NotificationsDialogMainCanvas.transform);
            var dialog = dialogGO.GetComponent<ConfirmationDialog>();

            ReadNextNotification(dialog);
        }
    }
#endif

		#region Auth Service Notifications Read Time
		private void ReadNextNotification(ConfirmationDialog dialog)
    {
        if(UnreadNotifications != null && UnreadNotifications.Any())
        {
            var notification = UnreadNotifications.First();
            dialog.Show(FormatNotification(notification), () =>
            {
                OnNotificationRead(notification);
                ReadNextNotification(dialog);
            });
        }
        else
        {
            Destroy(dialog.gameObject);
        }
    }

    public void OnNotificationRead(Notification notification)
    {
        UnreadNotifications.Remove(notification);
        long storedNotificationDate = GetLastNotificationReadDate();
        var notificationDate = long.Parse(notification.CreatedAt);
        if (notificationDate > storedNotificationDate)
        {
            SaveNotificationReadDate(notificationDate);
        }
    }

    private string FormatNotification(Notification notification) => $"{notification.Message}\n\nProject ID: {notification.ProjectId}\nCase ID: {notification.CaseId}";


	private const string NOTIFICATIONS_READ_KEY = "AUTH_SERVICE_NOTIFICATIONS_READ";
    private void SaveNotificationReadDate(long notificationReadDate)
    {
        PlayerPrefs.SetString(NOTIFICATIONS_READ_KEY, notificationReadDate.ToString());
    }

    private long GetLastNotificationReadDate()
    {
        if(long.TryParse(PlayerPrefs.GetString(NOTIFICATIONS_READ_KEY, "0"), out long result))
        {
            return result;
        }
        return 0;
    }
	#endregion
}
