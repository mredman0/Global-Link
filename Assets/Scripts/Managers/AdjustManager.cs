using AdjustSdk;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustManager : MonoBehaviour
{
    [Header("Settings")]
    public bool Sandbox;

    // Start is called before the first frame update
    void Start()
    {
#if UNITY_ANDROID
        var appToken = "rgspulciw2rk";
#elif UNITY_IOS
        var appToken = "zebbrtkumsjk";
#else
        return;
#endif
        AdjustConfig adjustConfig = new AdjustConfig(appToken, Sandbox ? AdjustEnvironment.Sandbox : AdjustEnvironment.Production);
        Adjust.InitSdk(adjustConfig);
    }
}
