using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class ConfigurationRetriever : IConfigurationRetriever
    {
        public GoogleDriveUser GetUserConfiguration(string userId)
        {
            return GetConfigurations().FirstOrDefault(user => user.MediaBrowserUserId == userId);
        }

        public GoogleDriveUser GetUserConfigurationById(string id)
        {
            return GetConfigurations().FirstOrDefault(user => user.Id == id);
        }

        public IEnumerable<GoogleDriveUser> GetConfigurations()
        {
            return Plugin.Instance.Configuration.Users;
        }

        public void SaveConfiguration()
        {
            Plugin.Instance.SaveConfiguration();
        }
    }
}
