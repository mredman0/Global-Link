using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public event Action<Vector2> Press;
    public event Action<Vector2> Release;
    public event Action<Vector2> Tap;
    public event Action<Vector2> Drag;
    public event Action<float> Rotate;

    [Header("Settings")]
    public float MaxDragForTap = 0.05f;
    public float MinDistanceForDrag = 0.03f;

    [Header("State")]
    public bool Pressed = false;
    public float PressTime = float.MinValue;
    public Vector2 PressStartPosition;
    public Vector2 PressLatestPosition;
    public float DistanceThisDrag = 0f;
    public bool TwoTouching = false;
    public float TwoTouchAngle;

    private List<(Component origin, Action action)> OnBackStack = new List<(Component origin, Action action)>();

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
        var previouslyTwoPressing = TwoTouching;
        var twoPressing = Input.touchCount > 1;

        if (twoPressing)
        {
            var t1 = Input.GetTouch(0);
            var t2 = Input.GetTouch(1);

            var diff = t1.position - t2.position;

            // This may needs some adjustment
            var currentAngle = Vector2.SignedAngle(diff, Vector2.right);

            if(TwoTouching)
            {
                var prev = TwoTouchAngle;
                if(currentAngle != prev)
                {
                    if(Mathf.Abs(prev) > 90f && Mathf.Abs(currentAngle) > 90f && Mathf.Sign(prev) != Mathf.Sign(currentAngle))
                    {
                        if(prev > 0)
                        {
                            prev = -360f + prev;
                        }
                        else
                        {
                            prev = 360f + prev;
                        }
                    }
                    Rotate?.Invoke(currentAngle - prev);
                }
            }
            TwoTouchAngle = currentAngle;
        }
        TwoTouching = twoPressing;

        if (pressing)
        {
            var position = Input.GetMouseButton(0) ? (Vector2)Input.mousePosition : Input.touches[0].position;
            if(TwoTouching)
            {
                position = (Input.touches[0].position + Input.touches[1].position) / 2f;
                if(!previouslyTwoPressing)
                {
                    // Reset data that would otherwise cause a jolting drag even due to just starting a two-touch
                    PressLatestPosition = position;
                }
            }
            else if(previouslyTwoPressing)
            {
                // Reset data that would otherwise cause a jolting drag even due to just starting a two-touch
                PressLatestPosition = position;
            }

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

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape) && OnBackStack.Any())
        {
            OnBackStack.Last().action();
        }
    }

    public void AddBackAction(Component origin, Action action) => OnBackStack.Add((origin, action));
    public void RemoveBackAction(Component origin) => OnBackStack.RemoveAll(tuple => tuple.origin == origin);
}
