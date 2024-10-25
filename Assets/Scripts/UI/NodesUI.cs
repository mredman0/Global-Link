using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NodesUI : MonoBehaviour
{
    [Header("Required References")]
    public Puzzle Puzzle;

    public HorizontalLayoutGroup Row;

    public GameObject NodeDisplayPrefab;

    public List<Sprite> ColorIconSprites;

    private Dictionary<int, GameObject> NodeDisplays = new Dictionary<int, GameObject>();
    private Dictionary<int, Image> NodeDisplayLineImages = new Dictionary<int, Image>();
    private Dictionary<int, List<Image>> NodeDisplayColorIcons = new Dictionary<int, List<Image>>();
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

        Puzzle.NodesConnected += OnNodesConnected;
        Puzzle.NodesDisconnected += OnNodesDisconnected;
    }

    private void OnDestroy()
    {
        SettingsManager.Instance.BoolSettingChanged -= OnBoolSettingChanged;

        Puzzle.PuzzleInitialized -= OnPuzzleInitialized;
        Puzzle.NodesConnected -= OnNodesConnected;
        Puzzle.NodesDisconnected -= OnNodesDisconnected;
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

    private void OnPuzzleInitialized()
    {
        foreach (var kvp in NodeDisplays)
        {
            Destroy(kvp.Value);
        }
        NodeDisplays.Clear();
        NodeDisplayLineImages.Clear();
        NodeDisplayColorIcons.Clear();

        foreach (var kvp in Puzzle.NodesByColor)
        {
            var color = kvp.Key;
            
            var nodesDisplayGO = Instantiate(NodeDisplayPrefab, Row.transform);
            var allImages = nodesDisplayGO.GetComponentsInChildren<Image>(includeInactive: true);
            var imagesToTint = allImages.Where(img => !img.CompareTag("Color Icon"));
            foreach(var img in imagesToTint)
            {
                img.color = ColorMapController.Instance.ApplyActiveColorMap(color);
            }
            var nodesDisplayLineImage = imagesToTint.First(img => img.gameObject.name.Contains("Line"));

            nodesDisplayLineImage.gameObject.SetActive(false);

            NodeDisplays.Add(color, nodesDisplayGO);
            NodeDisplayLineImages.Add(color, nodesDisplayLineImage);
            NodeDisplayColorIcons.Add(color, allImages.Where(img => img.CompareTag("Color Icon")).ToList());
        }

        UpdateColorIcons();
    }

    private void OnNodesConnected(Node a, Node b)
    {
        NodeDisplayLineImages[a.Color].gameObject.SetActive(true);
    }
    private void OnNodesDisconnected(Node a, Node b)
    {
        NodeDisplayLineImages[a.Color].gameObject.SetActive(false);
    }

    private void UpdateColorIcons()
    {
        foreach (var kvp in NodeDisplayColorIcons)
        {
            UpdateColorIcons(kvp.Key);
        }
    }

    private void UpdateColorIcons(int color)
    {
        if (ShowColorIcons)
        {
            foreach(var img in NodeDisplayColorIcons[color])
            {
                img.gameObject.SetActive(true);
                img.sprite = ColorIconSprites[color];
            }
        }
        else
        {
            foreach (var img in NodeDisplayColorIcons[color])
            {
                img.sprite = null;
                img.gameObject.SetActive(false);
            }
        }
    }
}
