using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialInstructionsProvider : MonoBehaviour
{
    public static TutorialInstructionsProvider Instance;

    public List<string> TutorialPuzzleIds;
    public List<GameObject> TutorialInstructionPrefabs;

    // Start is called before the first frame update
    void Start()
    {
        if (Instance)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public GameObject GetTutorialInstructionsPrefab(string puzzleId)
    {
        var index = TutorialPuzzleIds.IndexOf(puzzleId);
        if(index < 0)
        {
            return null;
        }
        return TutorialInstructionPrefabs[index];
    }
}
