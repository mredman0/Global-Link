using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BannerAdManager : MonoBehaviour
{
    public static BannerAdManager Instance;

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
    }
}
