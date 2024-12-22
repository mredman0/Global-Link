using Newtonsoft.Json;

namespace Global_Link_DailyPuzzleServer
{
	public class Config
	{
		public static ConfigData Current;

		/// <summary>
		/// Initializes the Config class by loading JSON data from a specified file.
		/// </summary>
		/// <param name="filePath">The path to the JSON configuration file.</param>
		public static void Initialize(string filePath)
		{
			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException("Configuration file not found.", filePath);
			}

			string json = File.ReadAllText(filePath);
			Current = JsonConvert.DeserializeObject<ConfigData>(json)
						  ?? throw new InvalidOperationException("Failed to parse configuration file.");
		}

		public class ConfigData
		{
			#region Server
			public int Server_Port;
			public double[] Server_TokenValidityDaysTable;
			#endregion

			#region DB
			public string DB_DataSource;
			#endregion
		}
	}
}
