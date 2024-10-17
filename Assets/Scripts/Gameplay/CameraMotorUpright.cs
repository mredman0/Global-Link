using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMotorUpright : MonoBehaviour
{
    public GameObject CameraArm;
    public GriddedPuzzle Puzzle;

    public bool Panning { get; set; } = false;

    private Camera Camera;

    public float PitchInvalidLow = 5f;
    public float PitchInvalidHigh = 5f;

    public float Speed = 8f;
    public float MaxSpeed = 1f;

    public float PanAmountThisDrag { get; set; } = 0f;

    // Start is called before the first frame update
    void Start()
    {
        Camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButton(0))
        {
            var mouseX = Input.GetAxis("Mouse X") * UserSettings.Instance.PanSpeed * Speed;
            var mouseY = Input.GetAxis("Mouse Y") * UserSettings.Instance.PanSpeed * Speed;

            if (mouseX > 0 || mouseY > 0)
            {
                Panning = true;
            }

            var motion = new Vector2(mouseX, mouseY);
            if(motion.magnitude > MaxSpeed)
            {
                motion *= MaxSpeed / motion.magnitude;
            }

            PanAmountThisDrag += motion.magnitude;

            // not sure why I have to do this
            var pitch = CameraArm.transform.localEulerAngles.x;
            motion.x *= Mathf.Cos(Mathf.Deg2Rad * pitch);

            //if(Puzzle.ActiveNode)
            //{
            //    motion *= -1;
            //}

            CameraArm.transform.Rotate(Vector3.up, motion.x);
            CameraArm.transform.Rotate(Vector3.right, motion.y);

            var rot = CameraArm.transform.localEulerAngles;
            rot.x = InverseClamp(rot.x, 90f-PitchInvalidLow, 270+PitchInvalidHigh);
            rot.z = 0;
            CameraArm.transform.rotation = Quaternion.Euler(rot);
        }
        else
        {
            Panning = false;
            PanAmountThisDrag = 0f;
        }
    }

    public void SnapToNode(Node n)
    {
        CameraArm.transform.LookAt(n.transform.position);
        var rot = CameraArm.transform.localEulerAngles;
        rot.x = InverseClamp(rot.x, 90f - PitchInvalidLow, 270 + PitchInvalidHigh);
        rot.z = 0;
        CameraArm.transform.rotation = Quaternion.Euler(rot);
    }

    private static float InverseClamp(float value, float lowBound, float highBound)
    {
        if(value <= lowBound || value >= highBound)
        {
            return value;
        }
        if(Mathf.Abs(value - lowBound) < Mathf.Abs(value - highBound))
        {
            return lowBound;
        }
        return highBound;
    }
}
