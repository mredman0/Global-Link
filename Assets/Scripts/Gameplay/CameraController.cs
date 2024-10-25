using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Required References")]
    public GameObject CameraArm;

    [Header("Optional References")]
    public Puzzle Puzzle;
    
    [Header("Generic Settings")]
    public float Speed = 8f;
    public float MaxSpeed = 1f;
    public float Friction = 1f;
    public bool AllowMomentumWithActiveNode = false;
    public int InputLocks = 0;
    public bool DoPuzzleCompleteSpin;

    private int DragInputsToStore = 5;

    [Header("Locked Roll Settings")]
    public bool AllowRoll = true;
    public float PitchInvalidLow = 5f;
    public float PitchInvalidHigh = 5f;

    public bool Panning { get; set; } = false;
    public float PanAmountThisDrag { get; set; } = 0f;
    public Camera Camera { get; set; }

    private List<Vector2> LastDragMotions = new List<Vector2>();
    private Vector2 Momentum;

    private bool InvertFreeLook;
    private bool InvertDrawing;
    private float Sensitivity;

    // Start is called before the first frame update
    void Start()
    {
        Camera = GetComponent<Camera>();

        SettingsManager.Instance.BoolSettingChanged += OnBoolSettingChanged;
        SettingsManager.Instance.FloatSettingChanged += OnFloatSettingChanged;

        InputManager.Instance.Press += OnPress;
        InputManager.Instance.Release += OnRelease;
        InputManager.Instance.Drag += OnDrag;

        if(Puzzle)
        {
            Puzzle.PuzzleCompleted += OnPuzzleCompleted;
        }

        InvertFreeLook = SettingsManager.Instance.GetBool(INVERT_FREE_LOOK_KEY);
        InvertDrawing = SettingsManager.Instance.GetBool(INVERT_DRAWING_KEY);
        Sensitivity = SettingsManager.Instance.GetFloat(SENSITIVITY_KEY);
    }

    private void OnDestroy()
    {
        SettingsManager.Instance.BoolSettingChanged -= OnBoolSettingChanged;
        SettingsManager.Instance.FloatSettingChanged -= OnFloatSettingChanged;

        InputManager.Instance.Press -= OnPress;
        InputManager.Instance.Release -= OnRelease;
        InputManager.Instance.Drag -= OnDrag;
        
        if(Puzzle)
        {
            Puzzle.PuzzleCompleted -= OnPuzzleCompleted;
        }
    }

    private const string INVERT_FREE_LOOK_KEY = "ControlsInvertFreeLook";
    private const string INVERT_DRAWING_KEY = "ControlsInvertDrawing";
    private void OnBoolSettingChanged(string setting, bool value)
    {
        if(setting == INVERT_FREE_LOOK_KEY)
        {
            InvertFreeLook = value;
        }
        else if(setting == INVERT_DRAWING_KEY)
        {
            InvertDrawing = value;
        }
    }

    private const string SENSITIVITY_KEY = "ControlsSensitivity";
    private void OnFloatSettingChanged(string setting, float value)
    {
        if(setting == SENSITIVITY_KEY)
        {
            Sensitivity = value;
        }
    }

    private void OnPress(Vector2 position)
    {
        if(InputLocks > 0)
        {
            return;
        }
        DoPuzzleCompleteSpin = false;
        Momentum = Vector2.zero;
    }

    private void OnRelease(Vector2 position)
    {
        if (InputLocks > 0)
        {
            return;
        }
        DoPuzzleCompleteSpin = false;
        var avgOfLatestDrags = Vector2.zero;
        foreach(var drag in LastDragMotions)
        {
            avgOfLatestDrags += drag;
        }
        avgOfLatestDrags /= DragInputsToStore;
        Momentum = avgOfLatestDrags;
        //if (Momentum.magnitude > MaxSpeed)
        //{
        //    Momentum *= MaxSpeed / Momentum.magnitude;
        //}
        LastDragMotions.Clear();
        Panning = false;
        PanAmountThisDrag = 0f;
    }

    private void OnDrag(Vector2 drag)
    {
        if (InputLocks > 0)
        {
            return;
        }
        DoPuzzleCompleteSpin = false;
        Panning = true;
        HandleDrag(drag);
    }

    private void OnPuzzleCompleted()
    {
        DoPuzzleCompleteSpin = true;
        Panning = false;
    }

    private void HandleDrag(Vector2 drag)
    {
        var motion = drag * Sensitivity * Speed;
        if (motion.magnitude > MaxSpeed)
        {
            motion *= MaxSpeed / motion.magnitude;
        }

        if(Puzzle.ActiveNode && InvertDrawing || !Puzzle.ActiveNode && InvertFreeLook)
        {
            motion *= -1f;
        }

        PanAmountThisDrag += motion.magnitude;

        if (!AllowRoll)
        {
            // not sure why I have to do this
            var pitch = CameraArm.transform.localEulerAngles.x;
            motion.x *= Mathf.Cos(Mathf.Deg2Rad * pitch);
        }

        LastDragMotions.Add(motion);
        if (LastDragMotions.Count > DragInputsToStore)
        {
            LastDragMotions.RemoveAt(0);
        }
        HandleMotion(motion);
    }

    private void HandleMotion(Vector2 motion)
    {
        CameraArm.transform.Rotate(Vector3.up, motion.x);
        CameraArm.transform.Rotate(Vector3.right, motion.y);

        var lastTryMotion = new Vector2(motion.x, motion.y);
        if (Puzzle)
        {
            var tryNum = 0;
            var maxTries = 6;
            var rotateEveryOtherTry = 18f;
            var currentRotate = 0f;
            while (!Puzzle.IsCameraPositionValid())
            {
                // Revert previous try
                CameraArm.transform.Rotate(Vector3.right, lastTryMotion.y * -1);
                CameraArm.transform.Rotate(Vector3.up, lastTryMotion.x * -1);

                if (tryNum > maxTries)
                {
                    break;
                }

                // Try new angle
                var rotationDirection = Mathf.Sign((tryNum % 2) - 0.5f);
                if (rotationDirection > 0)
                {
                    currentRotate += rotateEveryOtherTry;
                }
                lastTryMotion = Quaternion.Euler(0, 0, currentRotate * rotationDirection) * new Vector3(motion.x, motion.y, 0);
                CameraArm.transform.Rotate(Vector3.up, lastTryMotion.x);
                CameraArm.transform.Rotate(Vector3.right, lastTryMotion.y);

                tryNum++;
            }
        }

        if (!AllowRoll)
        {
            FixRoll();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(DoPuzzleCompleteSpin)
        {
            Momentum = Vector2.right * Mathf.Sin(Time.time + Mathf.PI/2f) + Vector2.up * Mathf.Cos(Time.time/2f);
        }
        if (!Panning)
        {
            if ((AllowMomentumWithActiveNode || !Puzzle || !Puzzle.ActiveNode) && (Momentum.x != 0 || Momentum.y != 0))
            {
                HandleMotion(Momentum);
                Momentum *= (1 - Mathf.Clamp(Friction * Time.fixedDeltaTime, 0, 1));
            }
        }
    }

    public void LockInput() => InputLocks++;
    public void FreeInput() => InputLocks = Mathf.Max(InputLocks - 1, 0);

    public void SnapToNodeEndOfPath(Node n)
    {
        if(!n.Path)
        {
            Debug.LogError($"{n} does not have a line to snap the camera to the end of!");
            return;
        }
        CameraArm.transform.LookAt(n.Path.GetPosition(n.Path.positionCount - 1), CameraArm.transform.up);
        if(!AllowRoll)
        {
            FixRoll();
        }
    }

    public void SnapToNode(Node n)
    {
        CameraArm.transform.LookAt(n.transform.position, CameraArm.transform.up);
        if(!AllowRoll)
        {
            FixRoll();
        }
    }

    public void SnapTo(Quaternion armRotation, float cameraDistance, float cameraFoV)
    {
        CameraArm.transform.rotation = armRotation;
        if(!AllowRoll)
        {
            FixRoll();
        }

        if(cameraDistance <= 0)
        {
            // Safe value
            cameraDistance = 5.73f;
        }
        if(cameraFoV <= 0)
        {
            // Safe value
            cameraFoV = 22.2f;
        }

        Camera.transform.localPosition = Vector3.forward * cameraDistance;
        var fovFixer = GetComponent<MaintainHorizontalFOV>();
        if(fovFixer)
        {
            fovFixer.desiredHorizontalFOV = cameraFoV;
            fovFixer.UpdateCameraFOV();
        }
        else
        {
            Camera.fieldOfView = cameraFoV;
        }
    }

    private void FixRoll()
    {
        var rot = CameraArm.transform.localEulerAngles;
        rot.x = InverseClamp(rot.x, 90f - PitchInvalidLow, 270 + PitchInvalidHigh);
        rot.z = 0;
        CameraArm.transform.rotation = Quaternion.Euler(rot);
    }

    private static float InverseClamp(float value, float lowBound, float highBound)
    {
        if (value <= lowBound || value >= highBound)
        {
            return value;
        }
        if (Mathf.Abs(value - lowBound) < Mathf.Abs(value - highBound))
        {
            return lowBound;
        }
        return highBound;
    }
}
