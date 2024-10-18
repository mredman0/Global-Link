using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserSettings : MonoBehaviour
{
    public static UserSettings Instance { get; set; }

    [Range(0.1f, 10f)]
    public float PanSpeed = 1;

    public string SelectedColorMap
    {
        get => ColorMapController.Instance.ActiveColorMap;
        set => ColorMapController.Instance.SetActiveColorMap(value);
    }

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetColorMap(string mapId)
    {
        SelectedColorMap = mapId;
    }
}
