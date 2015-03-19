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

        public GeneralConfiguration GetGeneralConfiguration()
        {
            return new GeneralConfiguration
            {
                GoogleDriveClientId = Configuration.GoogleDriveClientId,
                GoogleDriveClientSecret = Configuration.GoogleDriveClientSecret
            };
        }

        public GoogleDriveSyncAccount GetSyncAccount(string id)
        {
            return Configuration.SyncAccounts.FirstOrDefault(acc => acc.Id == id);
        }

        public IEnumerable<GoogleDriveSyncAccount> GetSyncAccounts()
        {
            return Configuration.SyncAccounts;
        }

        public IEnumerable<GoogleDriveSyncAccount> GetUserSyncAccounts(string userId)
        {
            return Configuration.SyncAccounts.Where(acc => acc.EnableForEveryone || acc.UserIds.Contains(userId));
        }

        public void AddSyncAccount(GoogleDriveSyncAccount syncAccount)
        {
            RemoveSyncAccount(syncAccount.Id);

            Configuration.SyncAccounts.Add(syncAccount);

            Plugin.Instance.SaveConfiguration();
        }

        public void RemoveSyncAccount(string id)
        {
            var existingAccountIndex = Configuration.SyncAccounts.FindIndex(acc => acc.Id == id);

            if (existingAccountIndex != -1)
            {
                Configuration.SyncAccounts.RemoveAt(existingAccountIndex);
            }

            Plugin.Instance.SaveConfiguration();
        }
    }
}
