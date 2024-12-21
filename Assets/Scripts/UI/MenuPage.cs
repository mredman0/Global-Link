using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MenuPage : MonoBehaviour
{
	public MenuManager MenuManager;

	public UnityEvent OnShown;
	public UnityEvent OnHidden;

	[Header("Back Behavior")]
	public MenuPage PreviousPage;
	public string PreviousScene;

	public void SetVisible(bool visible)
	{
		var previouslyShown = gameObject.activeSelf;
		gameObject.SetActive(visible);
		if(visible && !previouslyShown)
		{
			OnShown.Invoke();
		}
		else if(!visible && previouslyShown)
		{
			OnHidden.Invoke();
		}
	}

	public void GoBack()
	{
		if(PreviousPage)
		{
			MenuManager.GotoPage(PreviousPage);
			return;
		}
		if(!string.IsNullOrWhiteSpace(PreviousScene))
		{
			Addressables.LoadSceneAsync(PreviousScene);
			return;
		}

#if UNITY_EDITOR
		EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
	}
}
