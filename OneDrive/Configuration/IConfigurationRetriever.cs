using System.Collections.Generic;

namespace OneDrive.Configuration
{
    public interface IConfigurationRetriever
    {
        GeneralConfiguration GetGeneralConfiguration();
        OneDriveSyncAccount GetSyncAccount(string id);
        IEnumerable<OneDriveSyncAccount> GetSyncAccounts();
        IEnumerable<OneDriveSyncAccount> GetUserSyncAccounts(string userId);
        void AddSyncAccount(OneDriveSyncAccount syncAccount);
        void RemoveSyncAccount(string id);
    }
}
