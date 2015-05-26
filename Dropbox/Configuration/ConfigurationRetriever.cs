using System.Collections.Generic;
using System.Linq;

namespace Dropbox.Configuration
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
                DropboxAppKey = Configuration.DropboxAppKey,
                DropboxAppSecret = Configuration.DropboxAppSecret
            };
        }

        public DropboxSyncAccount GetSyncAccount(string id)
        {
            return Configuration.SyncAccounts.FirstOrDefault(acc => acc.Id == id);
        }

        public IEnumerable<DropboxSyncAccount> GetSyncAccounts()
        {
            return Configuration.SyncAccounts;
        }

        public IEnumerable<DropboxSyncAccount> GetUserSyncAccounts(string userId)
        {
            return Configuration.SyncAccounts.Where(acc => acc.EnableForEveryone || acc.UserIds.Contains(userId));
        }

        public void AddSyncAccount(DropboxSyncAccount syncAccount)
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
