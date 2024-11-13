using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PackInfo", order = 2)]
public class PackInfo : ScriptableObject
{
    public string Id;
    public string Name;
}
