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

	public int Port = 55611;

	private DailyPuzzleGenManager DailyPuzzleGenManager;

	public DailyPuzzleHttpServer()
	{
		DailyPuzzleGenManager = new DailyPuzzleGenManager();
	}

	public void StartServer()
	{
		var builder = WebApplication.CreateBuilder();

		builder.WebHost.ConfigureKestrel(options =>
		{
			options.Listen(IPAddress.Any, Port, listenOptions =>
			{
				listenOptions.UseHttps("cert.pfx", "12346");
			});
		});

		var app = builder.Build();
		
		BuildEndpoints(app);

#if DEBUG
		Console.WriteLine($"Starting Kestrel server on http://*:{Port}");
		app.Run($"http://*:{Port}");
#else
		Console.WriteLine($"Starting Kestrel server on https://*:{Port}");
		app.Run($"https://*:{Port}");
#endif
	}

	private void BuildEndpoints(WebApplication app)
	{
		app.MapGet("/Puzzles/Daily", Puzzles_Daily);
	}

	private async Task Puzzles_Daily(HttpRequest request, HttpResponse response)
	{
		// Extract "User-Id" from headers
		var userId = request.Headers["User-Id"].ToString() ?? "";

		// Prepare availability keys based on user purchases
		var puzzleAvailabilityKeys = new HashSet<int>();
		if (!string.IsNullOrEmpty(userId) && userId.Contains("WITH_PURCHASE"))
		{
			puzzleAvailabilityKeys.Add(1);
			puzzleAvailabilityKeys.Add(2);
			puzzleAvailabilityKeys.Add(3);
			puzzleAvailabilityKeys.Add(4);
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

		var puzzleList = DailyPuzzleGenManager.GetDailyPuzzles(requestedDate, puzzleAvailabilityKeys);

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

		Console.WriteLine($"Served {dayStr} daily puzzles for {userId}");
	}
}