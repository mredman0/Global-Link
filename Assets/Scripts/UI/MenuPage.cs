using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPage : MonoBehaviour
{
	public MenuManager MenuManager;
	
	[Header("Back Behavior")]
	public MenuPage PreviousPage;
	public string PreviousScene;

	public void SetVisible(bool visible)
	{
		gameObject.SetActive(visible);
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
			SceneManager.LoadScene(PreviousScene);
			return;
		}

#if UNITY_EDITOR
		EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
	}
}
