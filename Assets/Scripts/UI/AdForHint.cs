using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdForHint : MonoBehaviour
{
	[Header("Required References")]
	public Button Button;

	private void Start()
	{
		Button.interactable = AdManager.Instance.RewardedHintAvailable;

		AdManager.Instance.AdRewarded += OnAdRewarded;
		AdManager.Instance.RewardedHintLoaded += OnRewardedHintLoaded;
		AdManager.Instance.RewardedHintLoadFailed += OnRewardedHintLoadFailed;
		AdManager.Instance.RewardedHintClosed += OnAdClosed;
	}

	private void OnDestroy()
	{
		AdManager.Instance.AdRewarded -= OnAdRewarded;
		AdManager.Instance.RewardedHintLoaded -= OnRewardedHintLoaded;
		AdManager.Instance.RewardedHintLoadFailed -= OnRewardedHintLoadFailed;
		AdManager.Instance.RewardedHintClosed -= OnAdClosed;
	}

	public void ShowAdForHint()
	{
		Button.interactable = false;
		AdManager.Instance.ShowRewardedHint();
	}

	private void OnRewardedHintLoaded()
	{
		Button.interactable = true;
	}
	private void OnRewardedHintLoadFailed()
	{
		Button.interactable = false;
	}

	private const string HINT_REWARD_KEY = "Hint";
	private void OnAdRewarded(string reward, int amount)
	{
		if (reward == HINT_REWARD_KEY)
		{
			_ = HintManager.Instance.GainHints(amount);
			AdManager.Instance.LoadRewardedHint();
		}
	}
	private void OnAdClosed()
	{
		AdManager.Instance.LoadRewardedHint();
	}
}
