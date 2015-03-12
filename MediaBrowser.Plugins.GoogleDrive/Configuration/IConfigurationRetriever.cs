using System.Collections.Generic;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public interface IConfigurationRetriever
    {
        GoogleDriveUser GetUserConfiguration(string userId);
        IEnumerable<GoogleDriveUser> GetConfigurations();
        void SaveConfiguration();
    }
}
