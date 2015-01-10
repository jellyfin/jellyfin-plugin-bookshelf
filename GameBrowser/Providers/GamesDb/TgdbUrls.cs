
namespace GameBrowser.Providers.GamesDb
{
    public class TgdbUrls
    {
        public static string GetPlatform = @"http://thegamesdb.net/api/GetPlatform.php?id={0}";

        public const string GetGames = @"http://thegamesdb.net/api/GetGamesList.php?name={0}";
        public const string GetGamesByPlatform = @"http://thegamesdb.net/api/GetGamesList.php?name={0}&platform={1}";
        public const string GetInfo = @"http://thegamesdb.net/api/GetGame.php?id={0}";

        public const string BaseImagePath = @"http://thegamesdb.net/banners/";
    }
}
