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

    private Dictionary<int, GameObject> NodeDisplays = new Dictionary<int, GameObject>();
    private Dictionary<int, Image> NodeDisplayLineImages = new Dictionary<int, Image>();

    // Start is called before the first frame update
    void Start()
    {
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
        Puzzle.PuzzleInitialized -= OnPuzzleInitialized;
        Puzzle.NodesConnected -= OnNodesConnected;
        Puzzle.NodesDisconnected -= OnNodesDisconnected;
    }

    private void OnPuzzleInitialized()
    {
        foreach (var kvp in NodeDisplays)
        {
            Destroy(kvp.Value);
        }
        NodeDisplays.Clear();
        NodeDisplayLineImages.Clear();

        foreach (var kvp in Puzzle.NodesByColor)
        {
            var color = kvp.Key;
            
            var nodesDisplayGO = Instantiate(NodeDisplayPrefab, Row.transform);
            var imagesToTint = nodesDisplayGO.GetComponentsInChildren<Image>(includeInactive: true);
            foreach(var img in imagesToTint)
            {
                img.color = ColorMapController.Instance.ApplyActiveColorMap(color);
            }
            var nodesDisplayLineImage = imagesToTint.First(img => img.gameObject.name.Contains("Line"));

            nodesDisplayLineImage.gameObject.SetActive(false);

            NodeDisplays.Add(color, nodesDisplayGO);
            NodeDisplayLineImages.Add(color, nodesDisplayLineImage);
        }
    }

    private void OnNodesConnected(Node a, Node b)
    {
        NodeDisplayLineImages[a.Color].gameObject.SetActive(true);
    }
    private void OnNodesDisconnected(Node a, Node b)
    {
        NodeDisplayLineImages[a.Color].gameObject.SetActive(false);
    }
}
