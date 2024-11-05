using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuBackgroundRotator : MonoBehaviour
{
    public float Speed = 1f;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.forward, Time.deltaTime * Speed);
    }
}
