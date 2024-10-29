using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdForHint : MonoBehaviour
{
	public int HintsToReward = 1;

	public void ShowAdForHint()
	{
		Debug.Log("Play an ad here");
		HintManager.Instance.GainHints(HintsToReward);
	}
}
