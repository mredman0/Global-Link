using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lifetime : MonoBehaviour
{
    [Tooltip("Lifetime in seconds")]
    public float Duration = 5f;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, Duration);
    }
}
