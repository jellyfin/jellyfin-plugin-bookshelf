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

        public GoogleDriveUserDto GetUserConfiguration(string userId)
        {
            var googleDriveUser = GetUserConfigurationInternal(userId);

            return MapGoogleDriveUser(googleDriveUser);
        }

        public IEnumerable<GoogleDriveUserDto> GetConfigurations()
        {
            var googleDriveUsers = GetConfigurationsInternal();

            return googleDriveUsers.Select(MapGoogleDriveUser);
        }

        public void SaveUserConfiguration(string userId, AccessToken accessToken, string folderId)
        {
            var googleDriveUser = GetUserConfigurationInternal(userId);

            googleDriveUser.FolderId = folderId;
            googleDriveUser.AccessToken = accessToken;

            Plugin.Instance.SaveConfiguration();
        }

        private GoogleDriveUser GetUserConfigurationInternal(string userId)
        {
            if (Configuration.ApplyConfigurationToEveryone)
            {
                return Configuration.SingleUserForEveryone;
            }

            return GetConfigurationsInternal().FirstOrDefault(userDto => userDto.MediaBrowserUserId == userId);
        }

        private IEnumerable<GoogleDriveUser> GetConfigurationsInternal()
        {
            if (Configuration.ApplyConfigurationToEveryone)
            {
                return new List<GoogleDriveUser> { Configuration.SingleUserForEveryone };
            }

            return Configuration.Users;
        }

        private GoogleDriveUserDto MapGoogleDriveUser(GoogleDriveUser googleDriveUser)
        {
            return new GoogleDriveUserDto
            {
                GoogleDriveClientId = Configuration.GoogleDriveClientId,
                GoogleDriveClientSecret = Configuration.GoogleDriveClientSecret,
                User = googleDriveUser
            };
        }
    }
}
