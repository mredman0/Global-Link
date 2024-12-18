using Google.Apis.AndroidPublisher.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;

namespace Global_Link_DailyPuzzleServer
{
	public class GoogleStoreTokenValidator : TokenValidator
	{
		private readonly AndroidPublisherService _androidPublisherService;
		private readonly string _packageName = "com.redprismgames.chromasphere";

		private readonly string _gcloudCredentialsPath = "gcloudCredentials.json";

		public GoogleStoreTokenValidator()
		{
			// Authenticate using the service account JSON file
			var credential = GoogleCredential.FromFile(_gcloudCredentialsPath);
				//.CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

			// Initialize the Android Publisher API client
			_androidPublisherService = new AndroidPublisherService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "ChromaSphereServer",
			});
		}

		public override async Task<bool> ValidateTokenAsync(string productId, string purchaseToken)
		{
			try
			{
				// Call the purchases.products.get API to validate the token
				var request = _androidPublisherService.Purchases.Products.Get(_packageName, productId, purchaseToken);
				var purchase = await request.ExecuteAsync();

				// Check if the purchase state is valid
				// A valid purchase will have purchaseState = 0 (purchased), 1 (refunded), or 2 (canceled)
				if (purchase.PurchaseState == 0)
				{
					Console.WriteLine("Token is valid.");
					return true;  // The purchase is valid
				}

				Console.WriteLine("Token is invalid or refunded.");
				return false;  // The purchase is invalid or refunded
			}
			catch (Exception ex)
			{
				// Handle API errors or network issues
				Console.WriteLine("Error validating token: " + ex.Message);
				return false;
			}
		}
	}
}
