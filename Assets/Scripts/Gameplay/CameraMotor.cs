using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMotor : MonoBehaviour
{
    public GameObject Focus;
    public Puzzle Puzzle;

    public bool Panning { get; set; } = false;

    private Camera Camera;
    
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
            var mouseX = Input.GetAxis("Mouse X") * UserSettings.Instance.PanSpeed * Speed * Time.deltaTime;
            var mouseY = Input.GetAxis("Mouse Y") * UserSettings.Instance.PanSpeed * Speed * Time.deltaTime;

            if (mouseX > 0 || mouseY > 0)
            {
                Panning = true;
            }

            var motion = new Vector2(mouseX, mouseY);
            if(motion.magnitude > MaxSpeed)
            {
                motion *= MaxSpeed / motion.magnitude;
            }

            //if(Puzzle.ActiveNode)
            //{
            //    motion *= -1f;
            //}

            PanAmountThisDrag += motion.magnitude;

            transform.RotateAround(Focus.transform.position, transform.up, motion.x);
            transform.RotateAround(Focus.transform.position, transform.right, motion.y* -1);

            if(Puzzle)
            {
                var tryNum = 0;
                var maxTries = 6;
                var rotateEveryOtherTry = 18f;
                var currentRotate = 0f;
                var lastTryMotion = new Vector2(motion.x, motion.y);
                while(!Puzzle.IsCameraPositionValid())
                {
                    // Revert previous try
                    transform.RotateAround(Focus.transform.position, transform.right, lastTryMotion.y);
                    transform.RotateAround(Focus.transform.position, transform.up, lastTryMotion.x * -1);

                    if (tryNum > maxTries)
                    {
                        break;
                    }

                    // Try new angle
                    var rotationDirection = Mathf.Sign((tryNum % 2) - 0.5f);
                    if(rotationDirection > 0)
                    {
                        currentRotate += rotateEveryOtherTry;
                    }
                    lastTryMotion = Quaternion.Euler(0, 0, currentRotate*rotationDirection) * new Vector3(motion.x, motion.y, 0);
                    transform.RotateAround(Focus.transform.position, transform.up, lastTryMotion.x);
                    transform.RotateAround(Focus.transform.position, transform.right, lastTryMotion.y * -1);

                    tryNum++;
                }
            }

            if(Puzzle && !Puzzle.IsCameraPositionValid())
            {
                transform.RotateAround(Focus.transform.position, transform.right, motion.y);
                transform.RotateAround(Focus.transform.position, transform.up, motion.x* -1);
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
        var nodeDirection = n.transform.position - Focus.transform.position;
        var rotation = Quaternion.FromToRotation(transform.position - Focus.transform.position, nodeDirection);
        rotation.ToAngleAxis(out float angle, out Vector3 axis);
        transform.RotateAround(Focus.transform.position, axis, angle);
    }
}
