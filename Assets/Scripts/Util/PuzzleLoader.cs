using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class PuzzleLoader : MonoBehaviour
{
	public string PuzzlePack;
	public string PuzzleIdInPack;

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

		string resourcePath = $"{PuzzlePack}/{PuzzleIdInPack}.asset";
		var puzzleConfig = Addressables.LoadAssetAsync<PuzzleConfig>(resourcePath).WaitForCompletion();
		if(!puzzleConfig)
		{
			Debug.LogError($"No puzzle found at resource path: {resourcePath}");
			return;
		}
		PuzzleProvider.Instance.PuzzleConfig = puzzleConfig;
		Addressables.LoadSceneAsync("Puzzle");
	}
}
