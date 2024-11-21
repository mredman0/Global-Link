using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class PurchaseUIController : MonoBehaviour
{
    public PurchaseItem AdFreeItem;

    // Start is called before the first frame update
    void Start()
    {
        AdFreeItem.SetOwned(PurchaseManager.Instance.IsAdFreeOwned());

        if (Puzzle.Current)
        {
            Puzzle.Current.LockInput();
            Puzzle.Current.CameraController.LockInput();
        }

        InputManager.Instance.AddBackAction(this, HideStore);
    }

    private void OnDestroy()
    {
        InputManager.Instance.RemoveBackAction(this);
    }

    public void HideStore()
    {
        if (Puzzle.Current)
        {
            Puzzle.Current.FreeInput();
            Puzzle.Current.CameraController.FreeInput();
        }
        Destroy(gameObject);
    }

    #region Ad Free
    public void PurchaseAdFree()
    {
        if(PurchaseManager.Instance.PurchaseAdFree())
        {
            // TODO
            Debug.Log("Ad Free experience purchased");

            AdFreeItem.SetOwned(true);
        }
    }
	#endregion

	#region Hints
	public void PurchaseHints(int amount)
    {
        if(PurchaseManager.Instance.PurchaseHints(amount))
        {
            HintManager.Instance.GainHints(amount);
        }
    }
	#endregion
}
