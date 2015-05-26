using System.Collections.Generic;

namespace Dropbox.Configuration
{
    public interface IConfigurationRetriever
    {
        GeneralConfiguration GetGeneralConfiguration();
        DropboxSyncAccount GetSyncAccount(string id);
        IEnumerable<DropboxSyncAccount> GetSyncAccounts();
        IEnumerable<DropboxSyncAccount> GetUserSyncAccounts(string userId);
        void AddSyncAccount(DropboxSyncAccount syncAccount);
        void RemoveSyncAccount(string id);
    }
}
