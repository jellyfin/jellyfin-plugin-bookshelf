using FolderSync.Configuration;
using FolderSync.Data;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FolderSync
{
    public class SyncProvider : IServerSyncProvider
    {
        private readonly DataProvider _dataProvider;
        private readonly ILogger _logger;

        public SyncProvider(ILogger logger, IJsonSerializer json)
        {
            _logger = logger;
            _dataProvider = new DataProvider(logger, json);
        }

        public Task SendFile(string inputFile, string path, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.Copy(inputFile, path, true);

            }, cancellationToken);
        }

        public Task DeleteFile(string path, SyncTarget target, CancellationToken cancellationToken)
        {
            return Task.Run(() => File.Delete(path), cancellationToken);
        }

        public Task<Stream> GetFile(string path, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return Task.FromResult((Stream)File.OpenRead(path));
        }

        public string GetFullPath(IEnumerable<string> paths, SyncTarget target)
        {
            var account = GetSyncAccounts()
                .FirstOrDefault(i => string.Equals(i.Id, target.Id, StringComparison.OrdinalIgnoreCase));

            if (account == null)
            {
                throw new ArgumentException("Invalid SyncTarget supplied.");
            }

            var list = paths.ToList();
            list.Insert(0, account.Path);

            return Path.Combine(list.ToArray());
        }

        public string GetParentDirectoryPath(string path, SyncTarget target)
        {
            return Path.GetDirectoryName(path);
        }

        public Task<List<DeviceFileInfo>> GetFileSystemEntries(string path, SyncTarget target)
        {
            List<FileInfo> files;

            try
            {
                files = new DirectoryInfo(path).EnumerateFiles("*", SearchOption.TopDirectoryOnly).ToList();
            }
            catch (DirectoryNotFoundException)
            {
                files = new List<FileInfo>();
            }

            return Task.FromResult(files.Select(i => new DeviceFileInfo
            {
                Name = i.Name,
                Path = i.FullName

            }).ToList());
        }

        public ISyncDataProvider GetDataProvider()
        {
            return _dataProvider;
        }

        public string Name
        {
            get { return "Folder Sync"; }
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            return GetSyncAccounts()
                .Where(i => i.EnableAllUsers || i.UserIds.Contains(userId, StringComparer.OrdinalIgnoreCase))
                .Select(GetSyncTarget);
        }

        public IEnumerable<SyncTarget> GetAllSyncTargets()
        {
            return GetSyncAccounts().Select(GetSyncTarget);
        }

        private SyncTarget GetSyncTarget(SyncAccount account)
        {
            return new SyncTarget
            {
                Id = account.Id,
                Name = account.Name
            };
        }

        private IEnumerable<SyncAccount> GetSyncAccounts()
        {
            return Plugin.Instance.Configuration.SyncAccounts.ToList();
        }
    }
}
