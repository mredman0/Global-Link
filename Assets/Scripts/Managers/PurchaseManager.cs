using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class PurchaseManager : MonoBehaviour, IDetailedStoreListener
{
    public static PurchaseManager Instance;

	public event Action Initialized;
	public event Action InitializationFailed;

	public event Action<string, PurchaseEventArgs> PurchaseProcessed;
	public event Action<Product, PurchaseFailureReason> PurchaseFailed;

	public event Action<string> RestoreSucceeded;
	public event Action<string> RestoreFailed;

	public event Action<bool> AdFreeChanged;
	public event Action<int> HintsPurchased;
	public event Action DailyPuzzleAccessChanged;

	[Header("Settings")]
	public bool UseFakeStore;
	public FakeStoreUIMode FakeStoreUIMode = FakeStoreUIMode.DeveloperUser;

	[Header("State")]
	public bool IsInitialized = false;

	private IStoreController Controller;
	private IExtensionProvider Extensions;
	private SynchronizationContext MainThreadContext;

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
		MainThreadContext = SynchronizationContext.Current;

#if DEMO
		return;
#endif

		if (UnityServicesManager.Instance.Initialized)
		{
			Initialize();
		}
		UnityServicesManager.Instance.ServicesInitialized += Initialize;
    }

	private void Initialize()
	{
		if (SynchronizationContext.Current != MainThreadContext)
		{
			MainThreadContext.Post(_ => Initialize(), null);
			return;
		}

		Debug.Log("Initializing PurchaseManager");
		var purchasingModule = StandardPurchasingModule.Instance();
		if(UseFakeStore)
		{
			purchasingModule.useFakeStoreAlways = UseFakeStore;
			purchasingModule.useFakeStoreUIMode = FakeStoreUIMode;
			Debug.Log("Using Fake Store...");
		}

		var builder = ConfigurationBuilder.Instance(purchasingModule);
		builder.AddProducts(new List<ProductDefinition>()
		{
			new ProductDefinition($"{ID_PREFIX}ad_free", $"{ID_PREFIX}ad_free", ProductType.NonConsumable, true),

			new ProductDefinition($"{ID_PREFIX}hints_small", $"{ID_PREFIX}hints_small", ProductType.Consumable, true, new PayoutDefinition(PayoutType.Resource, "hint", 10)),
			new ProductDefinition($"{ID_PREFIX}hints_large", $"{ID_PREFIX}hints_large", ProductType.Consumable, true, new PayoutDefinition(PayoutType.Resource, "hint", 100)),

			new ProductDefinition($"{ID_PREFIX}daily_puzzles_beginner", $"{ID_PREFIX}daily_puzzles_beginner", ProductType.NonConsumable, true),
			new ProductDefinition($"{ID_PREFIX}daily_puzzles_intermediate", $"{ID_PREFIX}daily_puzzles_intermediate", ProductType.NonConsumable, true),
			new ProductDefinition($"{ID_PREFIX}daily_puzzles_expert", $"{ID_PREFIX}daily_puzzles_expert", ProductType.NonConsumable, true),
			new ProductDefinition($"{ID_PREFIX}daily_puzzles_grandmaster", $"{ID_PREFIX}daily_puzzles_grandmaster", ProductType.NonConsumable, true),
			new ProductDefinition($"{ID_PREFIX}daily_puzzles_all", $"{ID_PREFIX}daily_puzzles_all", ProductType.NonConsumable, true),
		});

		UnityPurchasing.Initialize(this, builder);
	}

	public const string ID_PREFIX = "com.redprismgames.chromasphere.";

#region IDetailedStoreListener
	public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
	{
		Debug.Log("UnityPurchasing initialized");
		IsInitialized = true;
		Controller = controller;
		Extensions = extensions;

		var adfreeProduct = Controller.products.WithID($"{ID_PREFIX}ad_free");
		if(adfreeProduct != null && adfreeProduct.hasReceipt)
		{
			Debug.Log("Ad-Free product owned on initialization, applying ad-free state.");
			AdFreeChanged?.Invoke(true);
		}

		Initialized?.Invoke();
	}
	public void OnInitializeFailed(InitializationFailureReason error)
	{
		Debug.LogError($"UnityPurchasing initialization failed: {error}");
		InitializationFailed?.Invoke();
	}
	public void OnInitializeFailed(InitializationFailureReason error, string message)
	{
		Debug.LogError($"UnityPurchasing initialization failed: {error}, {message}");
		InitializationFailed?.Invoke();
	}

	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
	{
		PurchaseProcessingResult result;
		var payouts = purchaseEvent.purchasedProduct.definition.payouts;
		var id = purchaseEvent.purchasedProduct.definition.id.Replace(ID_PREFIX, "");
		if (payouts != null && payouts.Any())
		{
			result = ProcessPurchaseByPayouts(payouts);
			PurchaseProcessed?.Invoke(id, purchaseEvent);
			return result;
		}
		result = ProcessPurchaseById(id);
		PurchaseProcessed?.Invoke(id, purchaseEvent);
		return result;
	}
	private PurchaseProcessingResult ProcessPurchaseByPayouts(IEnumerable<PayoutDefinition> payouts)
	{
		foreach(var payout in payouts)
		{
			if(payout.subtype == "hint")
			{
				Debug.Log($"Granting {(int)payout.quantity} hints from purchase");
				HintsPurchased?.Invoke((int)payout.quantity);
			}
		}
		return PurchaseProcessingResult.Complete;
	}
	private PurchaseProcessingResult ProcessPurchaseById(string id)
	{
		if(id == "ad_free")
		{
			Debug.Log($"Granting Ad-Free from purchase");
			AdFreeChanged?.Invoke(true);
			return PurchaseProcessingResult.Complete;
		}
		
		if(id == "daily_puzzles_beginner" ||
			id == "daily_puzzles_intermediate" ||
			id == "daily_puzzles_expert" ||
			id == "daily_puzzles_grandmaster" ||
			id == "daily_puzzles_all")
		{
			Debug.Log($"Daily puzzle access changed due to purchase");
			DailyPuzzleAccessChanged?.Invoke();
			return PurchaseProcessingResult.Complete;
		}

		return PurchaseProcessingResult.Complete;
	}

	public int CountAccessibleDailyPuzzles()
	{
		if(GetProduct("daily_puzzles_all")?.hasReceipt ?? false)
		{
			return 12;
		}
		var accessible = 4;
		if (GetProduct("daily_puzzles_beginner")?.hasReceipt ?? false) { accessible += 2; }
		if (GetProduct("daily_puzzles_intermediate")?.hasReceipt ?? false) { accessible += 2; }
		if (GetProduct("daily_puzzles_expert")?.hasReceipt ?? false) { accessible += 2; }
		if (GetProduct("daily_puzzles_grandmaster")?.hasReceipt ?? false) { accessible += 2; }
		return accessible;
	}

	public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
	{
		PurchaseFailed?.Invoke(product, failureDescription.reason);
	}
	public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
	{
		PurchaseFailed?.Invoke(product, failureReason);
	}
#endregion

#region API
	public void InitiatePurchase(string productId) => Controller.InitiatePurchase($"{ID_PREFIX}{productId}");

	public Product GetProduct(string productId) => Controller?.products?.WithID($"{ID_PREFIX}{productId}");

	public bool RestorePurchases()
	{
#if UNITY_EDITOR
		var success = UnityEngine.Random.Range(0, 2) % 2 == 0;
		if(success)
		{
			Debug.Log($"Transactions have been restored");
			RestoreSucceeded?.Invoke("Success");
		}
		else
		{
			Debug.LogError($"Failed to restore transactions");
			RestoreFailed?.Invoke("Failure");
		}
		return true;
#elif UNITY_IOS
		Extensions.GetExtension<IAppleExtensions>().RestoreTransactions((result, resultStr) => {
			if (result)
			{
				Debug.Log($"Transactions have been restored");
				RestoreSucceeded?.Invoke(resultStr);
			}
			else
			{
				Debug.LogError($"Failed to restore transactions: {resultStr}");
				RestoreFailed?.Invoke(resultStr);
			}
		});
		return true;
#elif UNITY_ANDROID
		Extensions.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions((result, resultStr) => {
			if (result)
			{
				Debug.Log($"Transactions have been restored");
				RestoreSucceeded?.Invoke(resultStr);
			}
			else
			{
				Debug.LogError($"Failed to restore transactions: {resultStr}");
				RestoreFailed?.Invoke(resultStr);
			}
		});
		return true;
#else
		return false;
#endif
	}
	#endregion
}
