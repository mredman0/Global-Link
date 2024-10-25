using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WaypointsUI : MonoBehaviour
{
    [Header("Required References")]
    public Puzzle Puzzle;

    public HorizontalLayoutGroup Row1;
    public HorizontalLayoutGroup Row2;

    public GameObject WaypointDisplayPrefab;

    public List<Sprite> ColorIconSprites;

    private Dictionary<Waypoint, Animator> WaypointDisplayAnimators = new Dictionary<Waypoint, Animator>();
    private Dictionary<Waypoint, Image> WaypointDisplayColorIcons = new Dictionary<Waypoint, Image>();
    private bool ShowColorIcons;

    // Start is called before the first frame update
    void Start()
    {
        ShowColorIcons = SettingsManager.Instance.GetBool(SHOW_COLOR_ICONS_KEY);
        SettingsManager.Instance.BoolSettingChanged += OnBoolSettingChanged;

        Puzzle.PuzzleInitialized += OnPuzzleInitialized;
        if (Puzzle.Initialized)
        {
            OnPuzzleInitialized();
        }

        Puzzle.WaypointColored += OnWaypointColored;
        Puzzle.WaypointUncolored += OnWaypointUncolored;
    }

    private void OnDestroy()
    {
        SettingsManager.Instance.BoolSettingChanged -= OnBoolSettingChanged;

        Puzzle.PuzzleInitialized -= OnPuzzleInitialized;
        Puzzle.WaypointColored -= OnWaypointColored;
        Puzzle.WaypointUncolored -= OnWaypointUncolored;
    }

    private const string SHOW_COLOR_ICONS_KEY = "AccessibilityShowColorIcons";
    private void OnBoolSettingChanged(string setting, bool value)
    {
        if (setting == SHOW_COLOR_ICONS_KEY)
        {
            ShowColorIcons = value;
            foreach (var kvp in WaypointDisplayColorIcons)
            {
                UpdateColorIcon(kvp.Key);
            }
        }
    }

    private void OnPuzzleInitialized()
    {
        foreach(var kvp in WaypointDisplayAnimators)
        {
            Destroy(kvp.Value.gameObject);
        }
        WaypointDisplayAnimators.Clear();
        WaypointDisplayColorIcons.Clear();
        int waypointsRendered = 0;
        foreach(var waypoint in Puzzle.Waypoints)
        {
            var waypointDisplayGO = Instantiate(WaypointDisplayPrefab, waypointsRendered < 3 ? Row1.transform : Row2.transform);
            waypointsRendered++;
            var waypointDisplay = waypointDisplayGO.GetComponent<Animator>();
            WaypointDisplayAnimators.Add(waypoint, waypointDisplay);
            WaypointDisplayColorIcons.Add(waypoint, waypointDisplayGO.GetComponentsInChildren<Image>().First(img => img.CompareTag("Color Icon")));
            WaypointDisplayColorIcons[waypoint].gameObject.SetActive(false);
        }
    }

    private void OnWaypointColored(Waypoint waypoint)
    {
        WaypointDisplayAnimators[waypoint].SetInteger("ColorIndex", waypoint.Color);
        UpdateColorIcon(waypoint);
    }

    private void OnWaypointUncolored(Waypoint waypoint)
    {
        WaypointDisplayAnimators[waypoint].SetInteger("ColorIndex", -1);
        UpdateColorIcon(waypoint);
    }

    private void UpdateColorIcon(Waypoint waypoint)
    {
        if(ShowColorIcons && waypoint.Color >= 0)
        {
            WaypointDisplayColorIcons[waypoint].gameObject.SetActive(true);
            WaypointDisplayColorIcons[waypoint].sprite = ColorIconSprites[waypoint.Color];
        }
        else
        {
            WaypointDisplayColorIcons[waypoint].sprite = null;
            WaypointDisplayColorIcons[waypoint].gameObject.SetActive(false);
        }
    }
}
