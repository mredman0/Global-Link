using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NodePairDisplay : MonoBehaviour
{
    [Header("Required References")]
    public Image LeftConnectedLine;
    public Image RightConnectedLine;
    public Image NoWaypoint;
    public Image Waypoint;
    public Image WaypointActive;
    public GameObject ColorSelectedUI;

    public List<Image> ColorIcons;
    public List<Sprite> ColorIconSprites;

    [Header("Settings")]
    public int Color;
    public bool HasWaypoint;

    [Header("State")]
    public bool NodesConnected;
    public bool WaypointReached;
    private bool ShowColorIcons;

    // Start is called before the first frame update
    void Start()
    {
        ShowColorIcons = SettingsManager.Instance.GetBool(SHOW_COLOR_ICONS_KEY);
        SettingsManager.Instance.BoolSettingChanged += OnBoolSettingChanged;
        UpdateColorIcons();
    }

    private void OnDestroy()
    {
        SettingsManager.Instance.BoolSettingChanged -= OnBoolSettingChanged;
    }

    private const string SHOW_COLOR_ICONS_KEY = "AccessibilityShowColorIcons";
    private void OnBoolSettingChanged(string setting, bool value)
    {
        if (setting == SHOW_COLOR_ICONS_KEY)
        {
            ShowColorIcons = value;
            UpdateColorIcons();
        }
    }

    public void SetColor(int color)
    {
        Color = color;
        var allImages = GetComponentsInChildren<Image>(includeInactive: true);
        var imagesToTint = allImages.Where(img => !img.CompareTag("Color Icon")).ToList();
        foreach (var img in imagesToTint)
        {
            img.color = ColorManager.Instance.GetColor(color);
        }
    }

    public void SetColorAndHasWaypoint(int color, bool hasWaypoint)
    {
        HasWaypoint = hasWaypoint;
        Waypoint.gameObject.SetActive(HasWaypoint);
        NoWaypoint.gameObject.SetActive(!HasWaypoint);

        SetColor(color);

        UpdateDisplay();
    }

    public void SetColorSelected(bool selected)
    {
        ColorSelectedUI.SetActive(selected);
    }

    public void SetNodesConnected(bool connected)
    {
        NodesConnected = connected;
        UpdateDisplay();
    }

    public void SetWaypointReached(bool reached)
    {
        WaypointReached = reached;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        LeftConnectedLine.gameObject.SetActive(NodesConnected || HasWaypoint && WaypointReached);
        RightConnectedLine.gameObject.SetActive(NodesConnected);
        NoWaypoint.gameObject.SetActive(!HasWaypoint && NodesConnected);
        WaypointActive.gameObject.SetActive(HasWaypoint && WaypointReached);
    }

    private void UpdateColorIcons()
    {
        if (ShowColorIcons)
        {
            foreach (var img in ColorIcons)
            {
                img.gameObject.SetActive(true);
                img.sprite = ColorIconSprites[Color];
            }
        }
        else
        {
            foreach (var img in ColorIcons)
            {
                img.sprite = null;
                img.gameObject.SetActive(false);
            }
        }
    }
}
