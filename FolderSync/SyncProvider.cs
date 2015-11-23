using FolderSync.Configuration;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using Interfaces.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Model.Logging;

namespace FolderSync
{
    public class SyncProvider : IServerSyncProvider, ISupportsDirectCopy
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public SyncProvider(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task<SyncedFileInfo> SendFile(Stream stream, string[] remotePath, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var fullPath = GetFullPath(remotePath, target);

            _fileSystem.CreateDirectory(Path.GetDirectoryName(fullPath));

            _logger.Debug("Folder sync saving stream to {0}", fullPath);
            
            using (var fileStream = _fileSystem.GetFileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
            {
                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                return GetSyncedFileInfo(fullPath);
            }
        }

        public Task DeleteFile(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                _fileSystem.DeleteFile(id);

                var account = GetSyncAccounts()
                    .FirstOrDefault(i => string.Equals(i.Id, target.Id, StringComparison.OrdinalIgnoreCase));

                if (account != null)
                {
                    try
                    {
                        DeleteEmptyFolders(account.Path);
                    }
                    catch
                    {
                    }
                }

            }, cancellationToken);
        }

        public Task<Stream> GetFile(string id, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return Task.FromResult(_fileSystem.OpenRead(id));
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

        public string Name
        {
            get { return Plugin.StaticName; }
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

        private void DeleteEmptyFolders(string parent)
        {
            foreach (var directory in _fileSystem.GetDirectoryPaths(parent))
            {
                DeleteEmptyFolders(directory);
                if (!_fileSystem.GetFileSystemEntryPaths(directory).Any())
                {
                    _fileSystem.DeleteDirectory(directory, false);
                }
            }
        }

        public Task<QueryResult<FileMetadata>> GetFiles(FileQuery query, SyncTarget target, CancellationToken cancellationToken)
        {
            var account = GetSyncAccounts()
                .FirstOrDefault(i => string.Equals(i.Id, target.Id, StringComparison.OrdinalIgnoreCase));

            if (account == null)
            {
                throw new ArgumentException("Invalid SyncTarget supplied.");
            }

            var result = new QueryResult<FileMetadata>();

            if (!string.IsNullOrWhiteSpace(query.Id))
            {
                var file = _fileSystem.GetFileSystemInfo(query.Id);

                if (file.Exists)
                {
                    result.TotalRecordCount = 1;
                    result.Items = new[] { file }.Select(GetFile).ToArray();
                }

                return Task.FromResult(result);
            }

            if (query.FullPath != null && query.FullPath.Length > 0)
            {
                var fullPath = GetFullPath(query.FullPath, target);
                var file = _fileSystem.GetFileSystemInfo(fullPath);

                if (file.Exists)
                {
                    result.TotalRecordCount = 1;
                    result.Items = new[] { file }.Select(GetFile).ToArray();
                }

                return Task.FromResult(result);
            }

            var files = _fileSystem.GetFiles(account.Path, true)
                .Select(GetFile)
                .ToArray();

            result.Items = files;
            result.TotalRecordCount = files.Length;

            return Task.FromResult(result);
        }

        private FileMetadata GetFile(FileSystemMetadata file)
        {
            return new FileMetadata
            {
                Id = file.FullName,
                Name = file.Name,
                MimeType = MimeTypes.GetMimeType(file.FullName)
            };
        }

        public Task<SyncedFileInfo> SendFile(string path, string[] pathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var fullPath = GetFullPath(pathParts, target);

            _fileSystem.CreateDirectory(Path.GetDirectoryName(fullPath));

            _logger.Debug("Folder sync copying file from {0} to {1}", path, fullPath);
            _fileSystem.CopyFile(path, fullPath, true);

            return Task.FromResult(GetSyncedFileInfo(fullPath));
        }

        private SyncedFileInfo GetSyncedFileInfo(string path)
        {
            // Normalize the full path to make sure it's consistent with the results you'd get from directory queries
            var file = _fileSystem.GetFileInfo(path);
            path = file.FullName;

            return new SyncedFileInfo
            {
                Path = path,
                Protocol = MediaProtocol.File,
                Id = path
            };
        }
    }
}
