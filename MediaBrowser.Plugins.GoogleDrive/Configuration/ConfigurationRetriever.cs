using System.Linq;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class ConfigurationRetriever : IConfigurationRetriever
    {
        public GoogleDriveUser GetUserConfiguration(string userId)
        {
            return Plugin.Instance.Configuration.Users.FirstOrDefault(user => user.MediaBrowserUserId == userId);
        }
    }
}
