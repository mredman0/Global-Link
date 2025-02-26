using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestReviewDialog : Dialog
{
	public void SendToStorePage()
	{
		ReviewRequestManager.Instance.SendToStoreForReview();
		Hide();
	}

	public void AskAgainLater()
	{
		Hide();
	}

	public void DoNotAskAgain()
	{
		ReviewRequestManager.Instance.StopAsking();
		Hide();
	}
}
