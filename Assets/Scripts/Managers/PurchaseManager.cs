using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchaseManager : MonoBehaviour
{
    public static PurchaseManager Instance;

    private IPurchaser Purchaser;

    // Start is called before the first frame update
    void Start()
    {
        if(Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
		DontDestroyOnLoad(gameObject);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD)
        Purchaser = new DevPurchaser();
#else
        Purchaser = new Purchaser();
#endif
    }

	#region API

	#region Ad Free
	public bool IsAdFreeOwned() => Purchaser.IsAdFreeOwned();
	public bool PurchaseAdFree() => Purchaser.PurchaseAdFree();
	#endregion

	#region Bonus Daily Puzzles
	public bool IsBonusDailiesOwned() => Purchaser.IsBonusDailiesOwned();
	public bool PurchaseBonusDailies() => Purchaser.PurchaseBonusDailies();
	#endregion

	#region Color Maps
	public bool IsColorMapOwned(string mapId) => Purchaser.IsColorMapOwned(mapId);
	public bool PurchaseColorMap(string mapId) => Purchaser.PurchaseColorMap(mapId);
	#endregion

	#region Hints
	public bool PurchaseHints(int amount) => Purchaser.PurchaseHints(amount);
	#endregion

	#region Packs
	public bool IsPackOwned(string packId) => Purchaser.IsPackOwned(packId);
	public bool PurchasePack(string packId) => Purchaser.PurchasePack(packId);
	#endregion

	#endregion
}
