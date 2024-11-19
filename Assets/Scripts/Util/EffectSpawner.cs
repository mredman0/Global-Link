using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    public GameObject Effect;

    public void SpawnEffect()
    {
        Instantiate(Effect);
    }
}
