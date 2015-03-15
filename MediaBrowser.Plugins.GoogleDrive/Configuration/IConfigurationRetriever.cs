using System.Collections.Generic;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public interface IConfigurationRetriever
    {
        GoogleDriveUserDto GetUserConfiguration(string userId);
        IEnumerable<GoogleDriveUserDto> GetConfigurations();
        void SaveUserConfiguration(string userId, AccessToken accessToken, string folderId);
    }
}
