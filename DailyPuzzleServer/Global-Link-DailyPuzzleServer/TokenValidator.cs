namespace Global_Link_DailyPuzzleServer
{
	public abstract class TokenValidator
	{
		public abstract Task<bool?> ValidateTokenAsync(string productId, string purchaseToken);
	}
}
