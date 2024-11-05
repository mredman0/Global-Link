using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevPurchaser : IPurchaser
{
	#region Storage Keys
	private const string PREFIX = "DEV-IAP-";
	private const string AD_FREE = PREFIX + "AdFree";
	private const string BONUS_DAILIES = PREFIX + "BonusDailies";
	private const string COLOR_MAP = PREFIX + "ColorMap";
	private const string PACK = PREFIX + "Pack";
	#endregion

	#region Ad Free

	public bool IsAdFreeOwned() => GetOwned(AD_FREE);
	public bool PurchaseAdFree()
	{
		SetOwned(AD_FREE);
		return true;
	}
	#endregion

	#region Bonus Daily Puzzles
	public bool IsBonusDailiesOwned() => GetOwned(BONUS_DAILIES);
	public bool PurchaseBonusDailies()
	{
		SetOwned(BONUS_DAILIES);
		return true;
	}
	#endregion

	#region Color Maps
	public bool IsColorMapOwned(string mapId) => GetOwned($"{COLOR_MAP}-{mapId}");
	public bool PurchaseColorMap(string mapId)
	{
		SetOwned($"{COLOR_MAP}-{mapId}");
		return true;
	}
	#endregion

	#region Hints
	private HashSet<int> SupportedHintAmounts = new HashSet<int>()
	{
		10, 100
	};
	public bool PurchaseHints(int amount) => SupportedHintAmounts.Contains(amount);
	#endregion

	#region Packs
	public bool IsPackOwned(string packId) => GetOwned($"{PACK}-{packId}");
	public bool PurchasePack(string packId)
	{
		SetOwned($"{PACK}-{packId}");
		return true;
	}
	#endregion

	#region Helper
	private bool GetOwned(string id) => PlayerPrefs.GetInt(id, 0) > 0;
	private void SetOwned(string id) => PlayerPrefs.SetInt(id, 1);
	#endregion
}
