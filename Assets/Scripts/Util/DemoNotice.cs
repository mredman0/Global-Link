using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoNotice : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if DEMO
        foreach(var component in GetComponents<MonoBehaviour>())
        {
            component.enabled = true;
        }
#endif
    }
}
