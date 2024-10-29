using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HintCounter : MonoBehaviour
{
    public TMP_Text CountText;

    // Start is called before the first frame update
    void Start()
    {
        HintManager.Instance.HintGained += OnHintGained;
        HintManager.Instance.HintUsed += OnHintUsed;
        OnHintCountChanged();
    }

    private void OnDestroy()
    {
        HintManager.Instance.HintGained -= OnHintGained;
        HintManager.Instance.HintUsed -= OnHintUsed;
    }

    private void OnHintGained() => OnHintCountChanged();
    private void OnHintUsed() => OnHintCountChanged();
    private void OnHintCountChanged()
    {
        CountText.text = HintManager.Instance.GetHints().ToString();
    }
}
