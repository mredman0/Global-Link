using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public event Action<Vector2> Press;
    public event Action<Vector2> Release;
    public event Action<Vector2> Tap;
    public event Action<Vector2> Drag;

    [Header("Settings")]
    public float MaxTimeForTap = 0.2f;
    public float MaxDragForTap = 50f;

    [Header("State")]
    public bool Pressed = false;
    public float PressTime = float.MinValue;
    public Vector2 PressStartPosition;
    public Vector2 PressLatestPosition;

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
        Input.simulateMouseWithTouches = false;
    }

    private void FixedUpdate()
    {
        var pressing = Input.GetMouseButton(0) || Input.touchCount > 0;

        if(pressing)
        {
            var position = Input.GetMouseButton(0) ? (Vector2)Input.mousePosition : Input.touches[0].position;
            if(Pressed)
            {
                Drag?.Invoke(position - PressLatestPosition);
            }
            PressLatestPosition = position;
            if (!Pressed)
            {
                PressTime = Time.realtimeSinceStartup;
                PressStartPosition = position;
                Pressed = true;
                Press?.Invoke(position);
            }
        }
        else
        {
            if(Pressed)
            {
                Pressed = false;
                Release?.Invoke(PressLatestPosition);
                if(Time.realtimeSinceStartup - PressTime <= MaxTimeForTap &&
                    Vector2.Distance(PressLatestPosition, PressStartPosition) < MaxDragForTap)
                {
                    Tap?.Invoke(PressLatestPosition);
                }
            }
        }
    }
}
