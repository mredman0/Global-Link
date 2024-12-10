using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class DailyPuzzlesPage : MonoBehaviour
{
    [Header("Required References")]
    public GameObject SectionPrefab;
    public Transform SectionContainer;

    [Header("Localization Lookup")]
    public PackInfo[] PackInfo;

    // Start is called before the first frame update
    void Start()
    {
        if(DailyPuzzleManager.Instance.PuzzlesAreReady)
        {
            OnDailyPuzzlesReady();
        }
        DailyPuzzleManager.Instance.DailyPuzzlesReady += OnDailyPuzzlesReady;
    }

    private void OnDestroy()
    {
        DailyPuzzleManager.Instance.DailyPuzzlesReady -= OnDailyPuzzlesReady;
    }

    private void OnDailyPuzzlesReady()
    {
        foreach(var kvp in DailyPuzzleManager.Instance.PuzzleGroups.OrderBy(kvp => kvp.Value.First()))
        {
            var sectionGO = Instantiate(SectionPrefab, SectionContainer);
            var section = sectionGO.GetComponent<DailyPuzzlesSection>();
            var packInfo = PackInfo.FirstOrDefault(p => p.Id == kvp.Key);
            section.Init(packInfo, kvp.Value);
        }
    }
}
