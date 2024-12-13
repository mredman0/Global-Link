using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using UnityEngine;

public class UnityServicesManager : MonoBehaviour
{
    public static UnityServicesManager Instance;

    public event Action ServicesInitialized;
    public event Action ServicesInitializationFailed;

    public bool Initialized = false;

    // Start is called before the first frame update
    void Start()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Debug.Log("Beginning Unity Services initialization...");
        UnityServices.InitializeAsync().ContinueWith(task => {
            if (task.IsCompleted)
            {
                Initialized = true;
                Debug.Log("Unity Services initialized");
                ServicesInitialized?.Invoke();
            }
            else
            {
                Initialized = false;
                Debug.LogError("Failed to initialize Unity Services");
                ServicesInitializationFailed?.Invoke();
            }
        });
    }
}
