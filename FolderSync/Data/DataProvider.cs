using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FolderSync.Data
{
    public class DataProvider : ISyncDataProvider
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;

        private readonly ConcurrentDictionary<string, TargetDataProvider> _providers =
            new ConcurrentDictionary<string, TargetDataProvider>(StringComparer.OrdinalIgnoreCase);

        public DataProvider(ILogger logger, IJsonSerializer json)
        {
            _logger = logger;
            _json = json;
        }

        private TargetDataProvider GetProvider(string id)
        {
            return _providers.GetOrAdd(id, key => new TargetDataProvider(id, _logger, _json));
        }

        public Task<List<string>> GetServerItemIds(SyncTarget target, string serverId)
        {
            return GetProvider(target.Id).GetServerItemIds(serverId);
        }

        public Task AddOrUpdate(SyncTarget target, LocalItem item)
        {
            return GetProvider(target.Id).AddOrUpdate(item);
        }

        public Task Delete(SyncTarget target, string id)
        {
            return GetProvider(target.Id).Delete(id);
        }

        public Task<LocalItem> Get(SyncTarget target, string id)
        {
            return GetProvider(target.Id).Get(id);
        }
    }
}
