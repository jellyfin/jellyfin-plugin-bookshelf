using System.Collections.Generic;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public interface IConfigurationRetriever
    {
        GoogleDriveUser GetUserConfiguration(string userId);
        GoogleDriveUser GetUserConfigurationById(string id);
        IEnumerable<GoogleDriveUser> GetConfigurations();
        void SaveConfiguration();
    }
}
