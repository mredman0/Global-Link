using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtSphereWithCameraUp : MonoBehaviour
{
    public Transform LookAt;

    private void Start()
    {
        LookAt = transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(LookAt, Camera.main.transform.up);
    }
}
