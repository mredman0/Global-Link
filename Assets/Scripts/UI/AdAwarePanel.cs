using com.unity3d.mediation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdAwarePanel : MonoBehaviour
{
    public bool BannerAdAware = true;
    public RectTransform BannerAdBackground;

    // Start is called before the first frame update
    void Start()
    {
        if(BannerAdAware)
        {
            AdjustOffsetMinForBanner();
            AdManager.Instance.BannerAdInitialized += AdjustOffsetMinForBanner;
        }
        AdManager.Instance.AdFreeChanged += OnAdFreeChanged;
    }

    private void OnDestroy()
    {
        AdManager.Instance.BannerAdInitialized -= AdjustOffsetMinForBanner;
        AdManager.Instance.AdFreeChanged -= OnAdFreeChanged;
    }

    private void OnAdFreeChanged(bool adFree)
    {
        AdjustOffsetMinForBanner();
    }

    private void AdjustOffsetMinForBanner()
    {
        var rect = GetComponent<RectTransform>();
        var offsetMin = rect.offsetMin;
        offsetMin.y = AdManager.Instance.BannerAdHeight;
        rect.offsetMin = offsetMin;

        if(BannerAdBackground)
        {
            var bannerAdOffsetMax = BannerAdBackground.offsetMax;
            bannerAdOffsetMax.y = AdManager.Instance.BannerAdHeight;
            BannerAdBackground.offsetMax = bannerAdOffsetMax;
        }
    }
}
