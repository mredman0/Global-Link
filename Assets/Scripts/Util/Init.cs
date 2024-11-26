using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Init : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if (SERVER)
        GetComponent<SceneLoader>().LoadScene("Daily Puzzle Provider");
#else
        GetComponent<SceneLoader>().LoadScene();
#endif
    }
}
