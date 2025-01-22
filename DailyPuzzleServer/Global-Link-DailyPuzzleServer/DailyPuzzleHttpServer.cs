using Global_Link_DailyPuzzleServer;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class DailyPuzzleHttpServer
{
	public const string REQUEST_DATE_FORMAT = "yyyy-MM-dd";

	private DailyPuzzleGenManager DailyPuzzleGenManager;

	private GoogleStoreTokenValidator GoogleStoreTokenValidator;
	private AppleStoreTokenValidator AppleStoreTokenValidator;

	public DailyPuzzleHttpServer()
	{
		DailyPuzzleGenManager = new DailyPuzzleGenManager();
		GoogleStoreTokenValidator = new GoogleStoreTokenValidator();
		AppleStoreTokenValidator = new AppleStoreTokenValidator();
	}

	public void StartServer()
	{
		var builder = WebApplication.CreateBuilder();

		builder.WebHost.ConfigureKestrel(options =>
		{
			options.Listen(IPAddress.Any, Config.Current.Server_Port, listenOptions =>
			{
				listenOptions.UseHttps("cert.pfx", "12346");
			});
		});
		builder.Logging.ClearProviders();

		var app = builder.Build();
		
		BuildEndpoints(app);

#if DEBUG
		Console.WriteLine($"Starting Kestrel server on http://*:{Config.Current.Server_Port}");
		app.Run($"http://*:{Config.Current.Server_Port}");
#else
		Console.WriteLine($"Starting Kestrel server on https://*:{Config.Current.Server_Port}");
		app.Run($"https://*:{Config.Current.Server_Port}");
#endif
	}

	private void BuildEndpoints(WebApplication app)
	{
		app.MapGet("/Puzzles/Daily", Puzzles_Daily);
	}

	private async Task Puzzles_Daily(HttpRequest request, HttpResponse response)
	{
		// Process provided purchase tokens
		TokenValidator purchaseValidator;
		if(!request.Headers.TryGetValue("Store-Type", out var storeTypeStr))
		{
			response.StatusCode = 400;
			await response.WriteAsync("Invalid Store-Type.");
			return;
		}
		var storeType = storeTypeStr.ToString();
		if (storeType == "Google")
		{
			purchaseValidator = GoogleStoreTokenValidator;
		}
		else if(storeType == "iOS" || storeType == "Apple")
		{
			purchaseValidator = AppleStoreTokenValidator;
		}
		else
		{
			response.StatusCode = 400;
			await response.WriteAsync($"Invalid Store-Type: \"{storeType}\"");
			return;
		}

		if(!request.Headers.TryGetValue("Purchase-Tokens", out var purchaseTokensStr))
		{
			purchaseTokensStr = "";
		}
		if (!request.Headers.TryGetValue("Product-Ids", out var productIdsStr))
		{
			productIdsStr = "";
		}

		var purchaseTokens = purchaseTokensStr.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
		var productIds = productIdsStr.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

		if(purchaseTokens.Length != productIds.Length)
		{
			response.StatusCode = 400;
			await response.WriteAsync("Purchase-Tokens/Product-Ids length must match");
			return;
		}

		var unlockedProducts = new List<int>();
		for(int i = 0; i < purchaseTokens.Length; i++)
		{
			var token = purchaseTokens[i];
			var productId = productIds[i];
			var hash = HashToken(token);

			var valid = DatabaseUtility.GetValidityForHash(hash, out DateTime? validUntil, out int? timesValidated);
			var shouldValidate = false;
			var isRevalidate = false;
			if(valid.HasValue)
			{
				if(!valid.Value)
				{
					// Record is explicitly marked as invalid, ignore it
					Log($"{hash} is invalid");
					continue;
				}
				else if(validUntil is null || (DateTime.UtcNow) > validUntil)
				{
					// Record is expired, we should revalidate
					Log($"{hash} is expired");
					shouldValidate = true;
					isRevalidate = true;
				}
				else
				{
					// Record is valid, add corresponding products
					Log($"{hash} is valid");
					AddProductCodes(ref unlockedProducts, productId);
				}
			}
			else
			{
				// Record does not yet exist, we should validate
				shouldValidate = true;
				isRevalidate = false;
			}

			if(shouldValidate)
			{
				bool? validated = await purchaseValidator.ValidateTokenAsync(productId, token);
				if(!validated.HasValue)
				{
					// In case we can't validate due to some error with the API, leave things in the state they were before
					// So if it was valid before, give them the benefit of the doubt until we try to validate again later
					if(valid.HasValue && valid.Value)
					{
						// Add corresponding products
						AddProductCodes(ref unlockedProducts, productId);
					}
				}
				else if (validated.Value)
				{
					if(isRevalidate)
					{
						var revalidatedValidUntil = DateTime.UtcNow + ValidityDurationBasedOnTrust(timesValidated ?? 0);
						Log($"Revalidated token will be considered valid for the next {(revalidatedValidUntil - DateTime.UtcNow).TotalHours} hours");
						DatabaseUtility.UpdateValidUntil(hash, revalidatedValidUntil, timesValidated.HasValue ? timesValidated.Value+1 : 1);
					}
					else
					{
						var newTokenValidUntil = DateTime.UtcNow + ValidityDurationBasedOnTrust(0);
						Log($"Newly validated token will be considered valid for the next {(newTokenValidUntil - DateTime.UtcNow).TotalHours} hours");
						DatabaseUtility.InsertTokenHash(hash, newTokenValidUntil, isValid: true);
					}
					// Add corresponding products
					AddProductCodes(ref unlockedProducts, productId);
				}
				else if(isRevalidate)
				{
					DatabaseUtility.InvalidateTokenHash(hash);
					continue;
				}
				else
				{
					// In this case, the token didn't exist yet, and we couldn't validate, so just ignore it
				}
			}
		}

		// Development backdoor
		bool allowOnDemand = false;
		if(request.Headers.TryGetValue("h8921rgh893wihgvi8w390hy9h2i389o3tr", out var devProductsStr))
		{
			allowOnDemand = true;
			var devProducts = devProductsStr.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
			foreach(var product in devProducts)
			{
				AddProductCodes(ref unlockedProducts, product);
			}
		}

		var puzzleAvailabilityKeys = new HashSet<int>();
		foreach(var key in unlockedProducts)
		{
			puzzleAvailabilityKeys.Add(key);
		}

		// Parse the "Request-Date" header
		var requestedDateStr = request.Headers["Request-Date"].ToString();
		var couldParse = DateTime.TryParseExact(
			requestedDateStr,
			REQUEST_DATE_FORMAT,
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out DateTime requestedDate
		);

		if (!couldParse ||
			requestedDate.Date < DateTime.Today.Date.AddDays(-1) ||
			requestedDate.Date > DateTime.Today.Date.AddDays(1))
		{
			requestedDate = DateTime.Today;
		}

		var puzzleList = DailyPuzzleGenManager.GetDailyPuzzles(requestedDate, puzzleAvailabilityKeys, allowOnDemand);

		// Prepare the response
		var payload = new PuzzlesPayload(puzzleList);
		response.ContentType = "application/json";
		await response.WriteAsync(JsonConvert.SerializeObject(payload));
		//await response.WriteAsJsonAsync(new { something = "hello" });

		// Log the request
		var dayStr = requestedDate.Date switch
		{
			var d when d == DateTime.Today.Date.AddDays(-1) => "yesterday's",
			var d when d == DateTime.Today.Date.AddDays(1) => "tomorrow's",
			_ => "today's"
		};

		Console.WriteLine($"Served {dayStr} daily puzzles, including products: {string.Join(',', unlockedProducts)}");
	}

	private const string PID_PREFIX = "com.redprismgames.chromasphere.";
	private Dictionary<string, List<int>> ProductIdToProductCodes = new Dictionary<string, List<int>>()
	{
		{ "daily_puzzles_beginner", new List<int>() { 1 } },
		{ "daily_puzzles_intermediate", new List<int>() { 2 } },
		{ "daily_puzzles_expert", new List<int>() { 3 } },
		{ "daily_puzzles_grandmaster", new List<int>() { 4 } },
		{ "daily_puzzles_all", new List<int>() { 1,2,3,4 } },
	};

	private void AddProductCodes(ref List<int> codes, string productId)
	{
		var id = productId.Replace(PID_PREFIX, "");
		if (!ProductIdToProductCodes.ContainsKey(id))
		{
			return;
		}
		codes.AddRange(ProductIdToProductCodes[id]);
	}

	private static TimeSpan ValidityDurationBasedOnTrust(int timesValidated)
	{
		var table = Config.Current.Server_TokenValidityDaysTable;
		var lookup = table[Math.Clamp(timesValidated, 0, table.Length-1)];
		return TimeSpan.FromDays(lookup);
	}

	private string HashToken(string token)
	{
		byte[] bytes = System.Text.Encoding.UTF8.GetBytes(token);
		return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(bytes));
	}

	private static void Log(string message)
	{
		Console.WriteLine($"[Server]: {message}");
	}
}