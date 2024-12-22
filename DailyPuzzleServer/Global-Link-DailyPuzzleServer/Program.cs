
using Global_Link_DailyPuzzleServer;

Config.Initialize("config.json");
DatabaseUtility.Initialize();

var server = new DailyPuzzleHttpServer();
server.StartServer();