using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPurchaser
{
	// Ad Free
	public bool IsAdFreeOwned();
	public bool PurchaseAdFree();

	// Bonus Daily Puzzles
	public bool IsBonusDailiesOwned();
	public bool PurchaseBonusDailies();

	// Color maps
	public bool IsColorMapOwned(string mapId);
	public bool PurchaseColorMap(string mapId);

	// Hints
	public bool PurchaseHints(int amount);

	// Packs
	public bool IsPackOwned(string packId);
	public bool PurchasePack(string packId);
}
