using System.Data.SQLite;

namespace Global_Link_DailyPuzzleServer
{
	public class DatabaseUtility
	{
		private static string ConnectionStringRead;
		private static string ConnectionStringReadWrite;

		public static void Initialize()
		{
			ConnectionStringRead = $"Data Source={Config.Current.DB_DataSource}";
			ConnectionStringReadWrite = $"Data Source={Config.Current.DB_DataSource};Mode=ReadWrite";
		}

		/// <summary>
		/// Inserts a new record into the Purchases table.
		/// </summary>
		/// <param name="tokenHash">The token hash to insert.</param>
		/// <param name="validUntil">The validation date as a Unix timestamp.</param>
		/// <param name="product">The product ID associated with this token hash.</param>
		/// <param name="isValid">Indicates whether the token hash is valid.</param>
		public static bool InsertTokenHash(string tokenHash, DateTime validUntil, bool isValid)
		{
			Log($"Inserting hash: {tokenHash}");
			long validUntilUnix = new DateTimeOffset(validUntil).ToUnixTimeSeconds();
			using (var connection = new SQLiteConnection(ConnectionStringReadWrite))
			{
				connection.Open();

				string query = "INSERT INTO Purchases (TokenHash, ValidUntil, IsValid, TimesValidated) " +
							   "VALUES (@TokenHash, @ValidUntil, @IsValid, @TimesValidated)";

				using (var command = new SQLiteCommand(query, connection))
				{
					command.Parameters.AddWithValue("@TokenHash", tokenHash);
					command.Parameters.AddWithValue("@ValidUntil", validUntilUnix);
					command.Parameters.AddWithValue("@IsValid", isValid ? 1 : 0);
					command.Parameters.AddWithValue("@TimesValidated", isValid ? 1 : 0);

					var success = command.ExecuteNonQuery() == 1;
					if (success)
					{
						Log($"{tokenHash} successfully inserted");
					}
					else
					{
						Log($"{tokenHash} could not be inserted");
					}
					return success;
				}
			}
		}

		/// <summary>
		/// Retrieves the product associated with a valid token hash.
		/// Returns -1 if the hash is invalid or not found.
		/// </summary>
		public static bool? GetValidityForHash(string tokenHash, out DateTime? validUntil, out int? timesValidated)
		{
			Log($"Fetching validity for hash: {tokenHash}");
			string query = "SELECT ValidUntil, IsValid, TimesValidated FROM Purchases WHERE TokenHash = @TokenHash";

			using (var connection = new SQLiteConnection(ConnectionStringRead))
			{
				connection.Open();
				using (var command = new SQLiteCommand(query, connection))
				{
					command.Parameters.AddWithValue("@TokenHash", tokenHash);
					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
						{
							Log($"Hash {tokenHash} found");
							validUntil = reader.IsDBNull(0) ? null : DateTime.UnixEpoch.AddSeconds(reader.GetInt64(0));
							timesValidated = reader.GetInt32(2);
							return reader.GetBoolean(1); // Return the IsValid value
						}
					}
				}
			}

			Console.WriteLine($"Hash {tokenHash} not found");
			validUntil = null;
			timesValidated = null;
			return null; // Return null if the record does not exist
		}

		public static bool UpdateValidUntil(string tokenHash, DateTime newValidUntil, int newTimesValidated)
		{
			Log($"Updating ValidUntil for hash: {tokenHash} (now validated {newTimesValidated} times)");
			string query = "UPDATE Purchases SET ValidUntil = @ValidUntil, TimesValidated = @TimesValidated WHERE TokenHash = @TokenHash";

			long newValidUntilUnix = new DateTimeOffset(newValidUntil).ToUnixTimeSeconds();
			using (var connection = new SQLiteConnection(ConnectionStringReadWrite))
			{
				connection.Open();
				using (var command = new SQLiteCommand(query, connection))
				{
					// Add parameters
					command.Parameters.AddWithValue("@ValidUntil", newValidUntilUnix);
					command.Parameters.AddWithValue("@TimesValidated", newTimesValidated);
					command.Parameters.AddWithValue("@TokenHash", tokenHash);

					// Execute the update
					var success = command.ExecuteNonQuery() == 1;
					if(success)
					{
						Log($"Last validated successfully updated for hash: {tokenHash}");
					}
					else
					{
						Log($"Last validated could not be updated for hash: {tokenHash}");
					}
					return success;
				}
			}
		}

		/// <summary>
		/// Invalidates the token hash by setting IsValid to 0.
		/// </summary>
		public static void InvalidateTokenHash(string tokenHash)
		{
			Log($"Invalidating hash: {tokenHash}");
			string query = "UPDATE Purchases SET IsValid = 0, TimesValidated = 0 WHERE TokenHash = @TokenHash";

			using (var connection = new SQLiteConnection(ConnectionStringReadWrite))
			{
				connection.Open();
				using (var command = new SQLiteCommand(query, connection))
				{
					command.Parameters.AddWithValue("@TokenHash", tokenHash);

					int rowsAffected = command.ExecuteNonQuery();
					Log(rowsAffected > 0
						? $"Token hash {tokenHash} invalidated successfully."
						: $"Token hash {tokenHash} not found or already invalid.");
				}
			}
		}

		private static void Log(string message)
		{
			Console.WriteLine($"[DB]: {message}");
		}
	}
}
