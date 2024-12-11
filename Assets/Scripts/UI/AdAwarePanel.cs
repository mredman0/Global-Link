using com.unity3d.mediation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdAwarePanel : MonoBehaviour
{
    public bool BannerAdAware = true;

    // Start is called before the first frame update
    void Start()
    {
        if(BannerAdAware)
        {
            AdjustOffsetMinForBanner();
        }
    }

    private void AdjustOffsetMinForBanner()
    {
        var rect = GetComponent<RectTransform>();
        var offsetMin = rect.offsetMin;
        offsetMin.y = AdManager.Instance.BannerAdHeight;
        rect.offsetMin = offsetMin;
    }
}
