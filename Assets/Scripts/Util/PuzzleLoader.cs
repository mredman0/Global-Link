using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class PuzzleLoader : MonoBehaviour
{
	[Header("Settings")]
	public string PuzzlePack;
	public string PuzzleIdInPack;
	public bool AllowInterstitial;


	private PuzzleConfig PuzzleConfig;

	public void LoadPuzzle()
	{
		if(!PuzzleProvider.Instance)
		{
			Debug.LogError($"No puzzle provider found when attempting to load puzzle");
			return;
		}

		if(string.IsNullOrWhiteSpace(PuzzlePack))
		{
			Debug.LogError($"PuzzlePack not set");
			return;
		}
		if (string.IsNullOrWhiteSpace(PuzzleIdInPack))
		{
			Debug.LogError($"PuzzleIdInPack not set");
			return;
		}

		if(PuzzlePack == "Daily")
		{
			PuzzleConfig = DailyPuzzleManager.Instance.DailyPuzzles[PuzzleIdInPack];
		}
		else
		{
			string resourcePath = $"{PuzzlePack}/{PuzzleIdInPack}.asset";
			PuzzleConfig = Addressables.LoadAssetAsync<PuzzleConfig>(resourcePath).WaitForCompletion();
			if (!PuzzleConfig)
			{
				Debug.LogError($"No puzzle found at resource path: {resourcePath}");
				return;
			}
		}

		if(AllowInterstitial)
		{
			AdManager.Instance.InterstitialClosed += DoLoad;
			if(!AdManager.Instance.ShowInterstitial())
			{
				AdManager.Instance.InterstitialClosed -= DoLoad;
				DoLoad();
			}
			return;
		}
		DoLoad();
	}

	private void DoLoad()
	{
		if(AllowInterstitial)
		{
			AdManager.Instance.InterstitialClosed -= DoLoad;
		}
		PuzzleProvider.Instance.PuzzleConfig = PuzzleConfig;
		Addressables.LoadSceneAsync("Puzzle");
	}
}
