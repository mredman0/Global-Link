using System.Collections;
using System.Collections.Generic;
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

    [Header("Locked Roll Settings")]
    public bool AllowRoll = true;
    public float PitchInvalidLow = 5f;
    public float PitchInvalidHigh = 5f;

    public bool Panning { get; set; } = false;
    public float PanAmountThisDrag { get; set; } = 0f;
    public Camera Camera { get; set; }

    private Vector2 Momentum;

    // Start is called before the first frame update
    void Start()
    {
        Camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            var mouseX = Input.GetAxis("Mouse X") * UserSettings.Instance.PanSpeed * Speed;
            var mouseY = Input.GetAxis("Mouse Y") * UserSettings.Instance.PanSpeed * Speed;

            if (mouseX > 0 || mouseY > 0)
            {
                Panning = true;
            }

            var motion = new Vector2(mouseX, mouseY);
            if (motion.magnitude > MaxSpeed)
            {
                motion *= MaxSpeed / motion.magnitude;
            }

            //if(Puzzle.ActiveNode)
            //{
            //    motion *= -1f;
            //}

            PanAmountThisDrag += motion.magnitude;

            if(!AllowRoll)
            {
                // not sure why I have to do this
                var pitch = CameraArm.transform.localEulerAngles.x;
                motion.x *= Mathf.Cos(Mathf.Deg2Rad * pitch);
            }

            CameraArm.transform.Rotate(Vector3.up, motion.x * Time.deltaTime);
            CameraArm.transform.Rotate(Vector3.right, motion.y * Time.deltaTime);

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
                    CameraArm.transform.Rotate(Vector3.right, lastTryMotion.y * -1 * Time.deltaTime);
                    CameraArm.transform.Rotate(Vector3.up, lastTryMotion.x * -1 * Time.deltaTime);

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
                    CameraArm.transform.Rotate(Vector3.up, lastTryMotion.x * Time.deltaTime);
                    CameraArm.transform.Rotate(Vector3.right, lastTryMotion.y * Time.deltaTime);

                    tryNum++;
                }
            }

            if(!Puzzle || !Puzzle.ActiveNode)
            {
                Momentum = lastTryMotion;
            }

            if(!AllowRoll)
            {
                FixRoll();
            }
        }
        else
        {
            Panning = false;
            PanAmountThisDrag = 0f;

            if((!Puzzle || !Puzzle.ActiveNode) && (Momentum.x != 0 || Momentum.y != 0))
            {
                CameraArm.transform.Rotate(Vector3.up, Momentum.x * Time.deltaTime);
                CameraArm.transform.Rotate(Vector3.right, Momentum.y * Time.deltaTime);

                Momentum *= (1 - Mathf.Clamp(Friction*Time.deltaTime, 0, 1));

                if (!AllowRoll)
                {
                    FixRoll();
                }
            }
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
            cameraFoV = 38.4f;
        }

        Camera.transform.localPosition = Vector3.forward * cameraDistance;
        Camera.fieldOfView = cameraFoV;
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
