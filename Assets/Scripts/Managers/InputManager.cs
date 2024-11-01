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
    public float MaxDragForTap = 0.05f;
    public float MinDistanceForDrag = 0.03f;

    [Header("State")]
    public bool Pressed = false;
    public float PressTime = float.MinValue;
    public Vector2 PressStartPosition;
    public Vector2 PressLatestPosition;
    public float DistanceThisDrag = 0f;

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

    public Vector2 NormalizeScreenPosition(Vector2 position) =>
        new Vector2(position.x / Screen.width, position.y / Screen.width);

    private void FixedUpdate()
    {
        var pressing = Input.GetMouseButton(0) || Input.touchCount > 0;

        if(pressing)
        {
            var position = Input.GetMouseButton(0) ? (Vector2)Input.mousePosition : Input.touches[0].position;
            var normalizedDragDistance = Vector2.Distance(NormalizeScreenPosition(position), NormalizeScreenPosition(PressLatestPosition));
            DistanceThisDrag += normalizedDragDistance;
            if (Pressed && DistanceThisDrag >= MinDistanceForDrag)
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
                DistanceThisDrag = 0f;
            }
        }
        else
        {
            if(Pressed)
            {
                Pressed = false;
                Release?.Invoke(PressLatestPosition);
                if (DistanceThisDrag < MaxDragForTap)
                {
                    Tap?.Invoke(PressLatestPosition);
                }
            }
        }
    }
}
