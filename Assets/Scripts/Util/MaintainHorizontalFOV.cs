using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaintainHorizontalFOV : MonoBehaviour
{
    public Camera Camera; // Reference to the Camera
    public float desiredHorizontalFOV = 60f; // Your desired horizontal FOV in degrees

    private int KnownScreenWidth = 0;
    private int KnownScreenHeight = 0;

    void Start()
    {
        UpdateCameraFOV();
    }

    void Update()
    {
        if(Screen.width != KnownScreenWidth || Screen.height != KnownScreenHeight)
        {
            KnownScreenWidth = Screen.width;
            KnownScreenHeight = Screen.height;
            UpdateCameraFOV();
        }
    }

    public void UpdateCameraFOV()
    {
        // Get the current aspect ratio
        float aspectRatio = (float)Screen.width / (float)Screen.height;

        // Calculate the vertical FOV based on the desired horizontal FOV
        float verticalFOV = 2f * Mathf.Atan(Mathf.Tan(desiredHorizontalFOV * 0.5f * Mathf.Deg2Rad) / aspectRatio) * Mathf.Rad2Deg;

        // Set the camera's vertical FOV
        Camera.fieldOfView = verticalFOV;
    }
}
