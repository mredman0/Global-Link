using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

public class Init : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_IOS
        var trackingAuthorizationStatus = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
        if (trackingAuthorizationStatus == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking();
            RequestedTime = Time.realtimeSinceStartup;
        }
#else
        GetComponent<SceneLoader>().LoadScene();
#endif
    }

#if UNITY_IOS
    private bool Continue = false;
    private float RequestedTime = 0;
    private void Update()
    {
        if(!Continue && (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED || Time.realtimeSinceStartup - RequestedTime > 20f))
        {
            Continue = true;
            GetComponent<SceneLoader>().LoadScene();
        }
    }
#endif
}
