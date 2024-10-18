using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMotor : MonoBehaviour
{
    [Header("Required References")]
    public GameObject CameraArm;

    [Header("Optional References")]
    public Puzzle Puzzle;
    
    [Header("Generic Settings")]
    public float Speed = 8f;
    public float MaxSpeed = 1f;

    [Header("Locked Roll Settings")]
    public bool AllowRoll = true;
    public float PitchInvalidLow = 5f;
    public float PitchInvalidHigh = 5f;

    public bool Panning { get; set; } = false;
    public float PanAmountThisDrag { get; set; } = 0f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            var mouseX = Input.GetAxis("Mouse X") * UserSettings.Instance.PanSpeed * Speed * Time.deltaTime;
            var mouseY = Input.GetAxis("Mouse Y") * UserSettings.Instance.PanSpeed * Speed * Time.deltaTime;

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

            CameraArm.transform.Rotate(Vector3.up, motion.x);
            CameraArm.transform.Rotate(Vector3.right, motion.y);

            if (Puzzle)
            {
                var tryNum = 0;
                var maxTries = 6;
                var rotateEveryOtherTry = 18f;
                var currentRotate = 0f;
                var lastTryMotion = new Vector2(motion.x, motion.y);
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

            if(!AllowRoll)
            {
                var rot = CameraArm.transform.localEulerAngles;
                rot.x = InverseClamp(rot.x, 90f - PitchInvalidLow, 270 + PitchInvalidHigh);
                rot.z = 0;
                CameraArm.transform.rotation = Quaternion.Euler(rot);
            }
        }
        else
        {
            Panning = false;
            PanAmountThisDrag = 0f;
        }
    }

    public void SnapToNode(Node n)
    {
        CameraArm.transform.LookAt(n.transform.position, CameraArm.transform.up);
        if(!AllowRoll)
        {
            var rot = CameraArm.transform.localEulerAngles;
            rot.x = InverseClamp(rot.x, 90f - PitchInvalidLow, 270 + PitchInvalidHigh);
            rot.z = 0;
            CameraArm.transform.rotation = Quaternion.Euler(rot);
        }
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
