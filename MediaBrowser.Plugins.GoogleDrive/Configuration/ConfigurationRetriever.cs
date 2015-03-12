using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class ConfigurationRetriever : IConfigurationRetriever
    {
        private static PluginConfiguration Configuration
        {
            get { return Plugin.Instance.Configuration; }
        }

        public GoogleDriveUser GetUserConfiguration(string userId)
        {
            if (Configuration.ApplyConfigurationToEveryone)
            {
                return Configuration.SingleUserForEveryone;
            }

            return GetConfigurations().FirstOrDefault(user => user.MediaBrowserUserId == userId);
        }

        public IEnumerable<GoogleDriveUser> GetConfigurations()
        {
            if (Configuration.ApplyConfigurationToEveryone)
            {
                return new List<GoogleDriveUser> { Configuration.SingleUserForEveryone };
            }

            return Configuration.Users;
        }

        public void SaveConfiguration()
        {
            Plugin.Instance.SaveConfiguration();
        }
    }
}
