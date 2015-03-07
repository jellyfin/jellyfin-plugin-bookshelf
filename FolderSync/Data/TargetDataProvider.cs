using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FolderSync.Data
{
    public class TargetDataProvider
    {
        private readonly string _targetId;

        private readonly SemaphoreSlim _dataLock = new SemaphoreSlim(1, 1);
        private List<LocalItem> _items;

        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;

        public TargetDataProvider(string targetId, ILogger logger, IJsonSerializer json)
        {
            _targetId = targetId;
            _logger = logger;
            _json = json;
        }

        private string GetDataPath()
        {
            return Path.Combine(Plugin.Instance.DataFolderPath, "data", _targetId);
        }

        private void EnsureData()
        {
            if (_items == null)
            {
                var path = GetDataPath();

                try
                {
                    _items = _json.DeserializeFromFile<List<LocalItem>>(path);
                }
                catch (DirectoryNotFoundException)
                {
                    _items = new List<LocalItem>();
                }
                catch (FileLoadException)
                {
                    _items = new List<LocalItem>();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error loading sync data from {0}", ex, path);
                    _items = new List<LocalItem>();
                }
            }
        }

        private void SaveData()
        {
            var path = GetDataPath();

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            try
            {
                _json.SerializeToFile(_items, path);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving sync data to {0}", ex, path);
            }
        }

        private async Task<T> GetData<T>(Func<List<LocalItem>, T> dataFactory)
        {
            await _dataLock.WaitAsync().ConfigureAwait(false);

            try
            {
                EnsureData();

                return dataFactory(_items);
            }
            finally
            {
                _dataLock.Release();
            }
        }

        private async Task UpdateData(Func<List<LocalItem>, List<LocalItem>> action)
        {
            await _dataLock.WaitAsync().ConfigureAwait(false);

            try
            {
                EnsureData();

                _items = action(_items);

                SaveData();
            }
            finally
            {
                _dataLock.Release();
            }
        }

        public Task<List<string>> GetServerItemIds(string serverId)
        {
            return GetData(items => items.Where(i => string.Equals(i.ServerId, serverId, StringComparison.OrdinalIgnoreCase)).Select(i => i.ItemId).ToList());
        }

        public Task AddOrUpdate(LocalItem item)
        {
            return UpdateData(items =>
            {
                var list = items.Where(i => !string.Equals(i.Id, item.Id, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                list.Add(item);

                return list;
            });
        }

        public Task Delete(string id)
        {
            return UpdateData(items => items.Where(i => !string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase)).ToList());
        }

        public Task<LocalItem> Get(string id)
        {
            return GetData(items => items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
