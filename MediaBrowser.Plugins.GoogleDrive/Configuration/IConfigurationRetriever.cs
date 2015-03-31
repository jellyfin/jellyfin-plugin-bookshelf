using System.Collections.Generic;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public interface IConfigurationRetriever
    {
        GeneralConfiguration GetGeneralConfiguration();
        GoogleDriveSyncAccount GetSyncAccount(string id);
        IEnumerable<GoogleDriveSyncAccount> GetSyncAccounts();
        IEnumerable<GoogleDriveSyncAccount> GetUserSyncAccounts(string userId);
        void AddSyncAccount(GoogleDriveSyncAccount syncAccount);
        void RemoveSyncAccount(string id);
    }
}
