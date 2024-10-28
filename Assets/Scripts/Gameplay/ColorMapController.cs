using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorMapController : MonoBehaviour
{
    public static ColorMapController Instance { get; set; }

    private const string RED = "Red";
    private const string GREEN = "Green";
    private const string BLUE = "Blue";
    private const string CYAN = "Cyan";
    private const string MAGENTA = "Magenta";
    private const string YELLOW = "Yellow";
    private const string ORANGE = "Orange";
    private const string LIME = "Lime";
    private const string GOLD = "Gold";
    private const string DARK_GOLD = "Dark Gold";
    private const string INDIGO = "Indigo";
    private const string VIOLET = "Violet";
    private const string RAINFOREST_GREEN = "Rainforest Green";

    private const string FLOW_RED = "Flow_Red";
    private const string FLOW_GREEN = "Flow_Green";
    private const string FLOW_BLUE = "Flow_Blue";
    private const string FLOW_CYAN = "Flow_Cyan";
    private const string FLOW_YELLOW = "Flow_Yellow";
    private const string FLOW_ORANGE = "Flow_Orange";

    public static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>()
    {
        { RED, Color.red },
        { GREEN, Color.green },
        { BLUE, Color.blue },
        { CYAN, Color.cyan },
        { MAGENTA, Color.magenta },
        { YELLOW, Color.yellow },
        { ORANGE, new Color(1, .43f, 0) },
        { LIME, new Color(.79f, 1, 0) },
        { GOLD, new Color(1, .78f, 0) },
        { DARK_GOLD, new Color(.8f, .62f, 0) },
        { INDIGO, new Color(.29f, 0, .51f) },
        { VIOLET, new Color(.5f, 0, 1) },
        { RAINFOREST_GREEN, new Color(0, .5f, .33f) },

        { FLOW_RED, new Color(.992f, 0.04f, 0) },
        { FLOW_GREEN, new Color(0, 0.553f, 0) },
        { FLOW_BLUE, new Color(0.051f, 0.157f, 0.996f) },
        { FLOW_CYAN, Color.cyan },
        { FLOW_YELLOW, new Color(0.918f, 0.871f, 0) },
        { FLOW_ORANGE, new Color(0.988f, 0.533f, 0) },
    };

    public static readonly Dictionary<string, Color[]> ColorMaps = new Dictionary<string, Color[]>()
    {
        { "Default", new Color[] {
            Colors[RED],
            Colors[BLUE],
            Colors[GREEN],
            Colors[CYAN],
            Colors[MAGENTA],
            Colors[YELLOW],
        }},
        //{ "Default", new Color[] {
        //    Colors[FLOW_RED],
        //    Colors[FLOW_BLUE],
        //    Colors[FLOW_GREEN],
        //    Colors[FLOW_CYAN],
        //    Colors[FLOW_YELLOW],
        //    Colors[FLOW_ORANGE],
        //}},
        { "Hot", new Color[] {
            Colors[RED],
            Colors[ORANGE],
            Colors[YELLOW],
            Colors[MAGENTA],
            Colors[GREEN],
            Colors[DARK_GOLD],
        }},
        { "Cool", new Color[] {
            Colors[BLUE],
            Colors[GREEN],
            Colors[CYAN],
            Colors[INDIGO],
            Colors[RAINFOREST_GREEN],
            Colors[VIOLET],
        }},
    };

    public string ActiveColorMap = "Default";
    public delegate void ColorMapChangedEvent();
    public event ColorMapChangedEvent ColorMapChanged;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {

    }


    public void SetActiveColorMap(string mapId)
    {
        if(mapId is null || !ColorMaps.ContainsKey(mapId))
        {
            Debug.LogError($"Could not set color map to {mapId}");
            return;
        }
        ActiveColorMap = mapId;
        ColorMapChanged?.Invoke();
    }

    public Color ApplyActiveColorMap(int colorIndex) => ColorMaps[ActiveColorMap][colorIndex];

    public string ColorName(int colorIndex)
    {
        var c = ColorMaps[ActiveColorMap][colorIndex];

        foreach(var kvp in Colors)
        {
            if(kvp.Value == c)
            {
                return kvp.Key;
            }
        }

        return c.ToString();
    }
}
