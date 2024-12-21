using UnityEngine;

public class MainMenuMainPage : MonoBehaviour
{
	public void RefetchDailyPuzzlesIfNewDay()
	{
		if(DailyPuzzleManager.Instance)
		{
			DailyPuzzleManager.Instance.RefetchPuzzlesIfNewDay();
		}
	}
}
