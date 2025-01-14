using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;

namespace Global_Link_DailyPuzzleServer
{
	public class AppleStoreTokenValidator : TokenValidator
	{
		private readonly string _serverApiUrl = "https://api.storekit.itunes.apple.com/inApps/v1/transactions/";
		private readonly string _sandboxServerApiUrl = "https://api.storekit-sandbox.itunes.apple.com/inApps/v1/transactions/";

		public override async Task<bool?> ValidateTokenAsync(string productId, string purchaseToken)
		{
			var transactionData = await GetTransactionDataAsync(purchaseToken, GetJwt());

			if (transactionData is null)
			{
				return null;
			}
			return IsTransactionValid(transactionData, productId);
		}

		public async Task<string> GetTransactionDataAsync(string transactionId, string jwt)
		{
			using HttpClient client = new HttpClient();
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

			HttpResponseMessage response = await client.GetAsync(_serverApiUrl + transactionId);
			if (response.IsSuccessStatusCode)
			{
				Console.WriteLine($"Success response {response.StatusCode} from Apple API (production)");
				var content = await response.Content.ReadAsStringAsync();
				if(content != null)
				{
					return content;  // Handle JSON response parsing
				}
			}
			else
			{
				Console.WriteLine($"Error getting Apple store (production) transaction data. tid={transactionId}, {response.StatusCode} - {response.ReasonPhrase}");
			}

			// Try sandbox environment
			response = await client.GetAsync(_sandboxServerApiUrl + transactionId);
			if (response.IsSuccessStatusCode)
			{
				Console.WriteLine($"Success response {response.StatusCode} from Apple API (sandbox)");
				var content = await response.Content.ReadAsStringAsync();
				if (content != null)
				{
					return content;  // Handle JSON response parsing
				}
			}
			else
			{
				Console.WriteLine($"Error getting Apple store (sandbox) transaction data. tid={transactionId}, {response.StatusCode} - {response.ReasonPhrase}");
			}
			return null;
		}

		public static bool IsTransactionValid(string jwsTransaction, string expectedProductId)
		{
			string payloadJson = DecodePayload(jwsTransaction);
			using JsonDocument jsonDoc = JsonDocument.Parse(payloadJson);

			var root = jsonDoc.RootElement;

			if (root.TryGetProperty("revocationDate", out JsonElement revocationDate) && revocationDate.ValueKind != JsonValueKind.Null)
			{
				// If revocationDate is present, the purchase was revoked.
				Console.WriteLine("Transaction has been revoked");
				return false;
			}

			if (root.TryGetProperty("expiresDate", out JsonElement expiresDate))
			{
				Console.WriteLine("Subscription has expired");
				long expiresDateMs = expiresDate.GetInt64();
				DateTime expirationDate = DateTimeOffset.FromUnixTimeMilliseconds(expiresDateMs).UtcDateTime;
				if (DateTime.UtcNow > expirationDate)
				{
					// If current date is past expiration, the subscription has expired.
					return false;
				}
			}

			if(!root.TryGetProperty("productId", out JsonElement productId) || productId.ValueKind != JsonValueKind.String || !productId.ValueEquals(expectedProductId))
			{
				// Transaction may be valid, but didn't match the suggested productId
				Console.WriteLine($"Transaction is valid but for the wrong product. Expected {expectedProductId} but found {productId}");
				return false;
			}

			// If no issues are found, assume the transaction is valid.
			return true;
		}

		private static string DecodePayload(string jwsTransaction)
		{
			string[] parts = jwsTransaction.Split('.');
			if (parts.Length != 3) throw new ArgumentException("Invalid JWT format in response.");

			string payloadBase64 = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
			byte[] payloadBytes = Convert.FromBase64String(payloadBase64);
			return Encoding.UTF8.GetString(payloadBytes);
		}

		#region JWT
		private readonly string _keyId = "JQZ7QHCTX3"; // From App Store Connect
		private readonly string _issuerId = "9dbca792-7d8a-4707-b7ca-1223811ada3e"; // From App Store Connect
		private readonly string _privateKeyPath = "appleKey.p8";

		private string _cachedJwt;
		private DateTime _expiryTime;
		private readonly object _lock = new object();

		public string GetJwt()
		{
			lock (_lock)
			{
				if (_cachedJwt == null || DateTime.UtcNow >= _expiryTime)
				{
					_cachedJwt = GenerateJwt();
					_expiryTime = DateTime.UtcNow.AddMinutes(49); // Slightly less than 50 minutes to be safe
				}
				return _cachedJwt;
			}
		}

		public string GenerateJwt()
		{
			var privateKey = File.ReadAllText(_privateKeyPath);
			if (string.IsNullOrWhiteSpace(privateKey))
			{
				return null;
			}
			var securityKey = new ECDsaSecurityKey(ECDsa.Create(ECCurve.NamedCurves.nistP256))
			{
				KeyId = _keyId
			};
			securityKey.ECDsa.ImportFromPem(privateKey.ToCharArray());

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Issuer = _issuerId,
				Audience = "appstoreconnect-v1",
				SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256),
				Claims = new Dictionary<string, object> {
						{ "bid", "com.redprismgames.chromasphere" },
					},
				Expires = DateTime.UtcNow.AddMinutes(50),
			};

			var tokenHandler = new JsonWebTokenHandler();
			return tokenHandler.CreateToken(tokenDescriptor);
		}
		#endregion
	}
}
