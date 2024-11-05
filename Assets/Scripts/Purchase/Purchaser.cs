using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Purchaser : IPurchaser
{
	#region Ad Free
	public bool IsAdFreeOwned()
	{
		throw new System.NotImplementedException();
	}
	public bool PurchaseAdFree()
	{
		throw new System.NotImplementedException();
	}
	#endregion

	#region Bonus Daily Puzzles
	public bool IsBonusDailiesOwned()
	{
		throw new System.NotImplementedException();
	}
	public bool PurchaseBonusDailies()
	{
		throw new System.NotImplementedException();
	}
	#endregion

	#region Color Maps
	public bool IsColorMapOwned(string mapId)
	{
		throw new System.NotImplementedException();
	}
	public bool PurchaseColorMap(string mapId)
	{
		throw new System.NotImplementedException();
	}
	#endregion

	#region Hints
	public bool PurchaseHints(int amount)
	{
		throw new System.NotImplementedException();
	}
	#endregion

	#region Packs
	public bool IsPackOwned(string packId)
	{
		throw new System.NotImplementedException();
	}
	public bool PurchasePack(string packId)
	{
		throw new System.NotImplementedException();
	}
	#endregion
}
