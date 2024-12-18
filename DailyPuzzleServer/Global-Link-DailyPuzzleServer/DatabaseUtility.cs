using System.Data.SQLite;

namespace Global_Link_DailyPuzzleServer
{
	public class DatabaseUtility
	{
		private const string DatabasePath = "Data Source=../ChromaSphereServerDB/purchases.db"; // Path to your SQLite database

		/// <summary>
		/// Inserts a new record into the TokenHashes table.
		/// </summary>
		/// <param name="tokenHash">The token hash to insert.</param>
		/// <param name="validationDate">The validation date as a Unix timestamp.</param>
		/// <param name="product">The product ID associated with this token hash.</param>
		/// <param name="isValid">Indicates whether the token hash is valid.</param>
		public static bool InsertTokenHash(string tokenHash, DateTime validationDate, bool isValid)
		{
			long unixTimestamp = new DateTimeOffset(validationDate).ToUnixTimeSeconds();
			using (var connection = new SQLiteConnection(DatabasePath))
			{
				connection.Open();

				string query = "INSERT INTO TokenHashes (TokenHash, ValidationDate, IsValid) " +
							   "VALUES (@TokenHash, @ValidationDate, @IsValid)";

				using (var command = new SQLiteCommand(query, connection))
				{
					command.Parameters.AddWithValue("@TokenHash", tokenHash);
					command.Parameters.AddWithValue("@ValidationDate", unixTimestamp);
					command.Parameters.AddWithValue("@IsValid", isValid ? 1 : 0);

					return command.ExecuteNonQuery() == 1;
				}
			}
		}

		/// <summary>
		/// Retrieves the product associated with a valid token hash.
		/// Returns -1 if the hash is invalid or not found.
		/// </summary>
		public static bool? GetValidityForHash(string tokenHash, out DateTime? lastValidated)
		{
			string query = "SELECT ValidationDate, IsValid FROM TokenHashes WHERE TokenHash = @TokenHash";

			using (var connection = new SQLiteConnection(DatabasePath))
			{
				connection.Open();
				using (var command = new SQLiteCommand(query, connection))
				{
					command.Parameters.AddWithValue("@TokenHash", tokenHash);
					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
						{
							lastValidated = reader.IsDBNull(0) ? (DateTime?)null : DateTime.UnixEpoch.AddSeconds(reader.GetInt64(0));
							return reader.GetBoolean(1); // Return the IsValid value
						}
					}
				}
			}

			lastValidated = null;
			return null; // Return null if the record does not exist
		}

		public static bool UpdateLastValidated(string tokenHash, DateTime newValidationDate)
		{
			string query = "UPDATE TokenHashes SET ValidationDate = @LastValidated WHERE TokenHash = @TokenHash";

			long unixTimestamp = new DateTimeOffset(newValidationDate).ToUnixTimeSeconds();
			using (var connection = new SQLiteConnection(DatabasePath))
			{
				connection.Open();
				using (var command = new SQLiteCommand(query, connection))
				{
					// Add parameters
					command.Parameters.AddWithValue("@LastValidated", unixTimestamp);
					command.Parameters.AddWithValue("@TokenHash", tokenHash);

					// Execute the update
					return command.ExecuteNonQuery() == 1;
				}
			}
		}

		/// <summary>
		/// Invalidates the token hash by setting IsValid to 0.
		/// </summary>
		public static void InvalidateTokenHash(string tokenHash)
		{
			string query = "UPDATE TokenHashes SET IsValid = 0 WHERE TokenHash = @TokenHash";

			using (var connection = new SQLiteConnection(DatabasePath))
			{
				connection.Open();
				using (var command = new SQLiteCommand(query, connection))
				{
					command.Parameters.AddWithValue("@TokenHash", tokenHash);

					int rowsAffected = command.ExecuteNonQuery();
					Console.WriteLine(rowsAffected > 0
						? $"Token hash '{tokenHash}' invalidated successfully."
						: $"Token hash '{tokenHash}' not found or already invalid.");
				}
			}
		}
	}
}
