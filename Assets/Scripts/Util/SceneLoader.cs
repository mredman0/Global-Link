using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string DefaultSceneToLoad;
    public bool Additive = false;

    public void LoadScene(string name = "")
    {
        if(string.IsNullOrWhiteSpace(name))
        {
            name = DefaultSceneToLoad;
        }
        SceneManager.LoadScene(name, Additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
    }
}
