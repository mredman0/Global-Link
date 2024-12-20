using System.Data.SQLite;

namespace Global_Link_DailyPuzzleServer
{
	public class DatabaseUtility
	{
		private const string DbPath = "Data Source=../ChromaSphereServerDB/purchases.db";
		private const string DbPathReadWrite = "Data Source=../ChromaSphereServerDB/purchases.db;Mode=ReadWrite";

		/// <summary>
		/// Inserts a new record into the TokenHashes table.
		/// </summary>
		/// <param name="tokenHash">The token hash to insert.</param>
		/// <param name="validationDate">The validation date as a Unix timestamp.</param>
		/// <param name="product">The product ID associated with this token hash.</param>
		/// <param name="isValid">Indicates whether the token hash is valid.</param>
		public static bool InsertTokenHash(string tokenHash, DateTime validationDate, bool isValid)
		{
			Log($"Inserting hash: {tokenHash}");
			long unixTimestamp = new DateTimeOffset(validationDate).ToUnixTimeSeconds();
			using (var connection = new SQLiteConnection(DbPathReadWrite))
			{
				connection.Open();

				string query = "INSERT INTO TokenHashes (TokenHash, ValidationDate, IsValid) " +
							   "VALUES (@TokenHash, @ValidationDate, @IsValid)";

				using (var command = new SQLiteCommand(query, connection))
				{
					command.Parameters.AddWithValue("@TokenHash", tokenHash);
					command.Parameters.AddWithValue("@ValidationDate", unixTimestamp);
					command.Parameters.AddWithValue("@IsValid", isValid ? 1 : 0);

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
		public static bool? GetValidityForHash(string tokenHash, out DateTime? lastValidated)
		{
			Log($"Fetching validity for hash: {tokenHash}");
			string query = "SELECT ValidationDate, IsValid FROM TokenHashes WHERE TokenHash = @TokenHash";

			using (var connection = new SQLiteConnection(DbPath))
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
							lastValidated = reader.IsDBNull(0) ? null : DateTime.UnixEpoch.AddSeconds(reader.GetInt64(0));
							return reader.GetBoolean(1); // Return the IsValid value
						}
					}
				}
			}

			Console.WriteLine($"Hash {tokenHash} not found");
			lastValidated = null;
			return null; // Return null if the record does not exist
		}

		public static bool UpdateLastValidated(string tokenHash, DateTime newValidationDate)
		{
			Log($"Updating last validated for hash: {tokenHash}");
			string query = "UPDATE TokenHashes SET ValidationDate = @LastValidated WHERE TokenHash = @TokenHash";

			long unixTimestamp = new DateTimeOffset(newValidationDate).ToUnixTimeSeconds();
			using (var connection = new SQLiteConnection(DbPathReadWrite))
			{
				connection.Open();
				using (var command = new SQLiteCommand(query, connection))
				{
					// Add parameters
					command.Parameters.AddWithValue("@LastValidated", unixTimestamp);
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
			string query = "UPDATE TokenHashes SET IsValid = 0 WHERE TokenHash = @TokenHash";

			using (var connection = new SQLiteConnection(DbPathReadWrite))
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
