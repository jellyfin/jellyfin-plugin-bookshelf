using FolderSync.Configuration;
using MediaBrowser.Common.IO;
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

namespace FolderSync
{
    public class SyncProvider : IServerSyncProvider
    {
        private readonly IFileSystem _fileSystem;

        public SyncProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task<SyncedFileInfo> SendFile(Stream stream, string[] remotePath, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var fullPath = GetFullPath(remotePath, target);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            using (var fileStream = _fileSystem.GetFileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
            {
                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                return new SyncedFileInfo
                {
                    Path = fullPath,
                    Protocol = MediaProtocol.File,
                    Id = fullPath
                };
            }
        }

        public Task DeleteFile(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                File.Delete(id);

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
            return Task.FromResult((Stream)File.OpenRead(id));
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

        private static void DeleteEmptyFolders(string parent)
        {
            foreach (var directory in Directory.GetDirectories(parent))
            {
                DeleteEmptyFolders(directory);
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory, false);
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
                var file = _fileSystem.GetFileSystemInfo(query.FullPath[0]);

                if (file.Exists)
                {
                    result.TotalRecordCount = 1;
                    result.Items = new[] { file }.Select(GetFile).ToArray();
                }

                return Task.FromResult(result);
            }

            var files = new DirectoryInfo(account.Path).EnumerateFiles("*", SearchOption.AllDirectories)
                .Select(GetFile)
                .ToArray();

            result.Items = files;
            result.TotalRecordCount = files.Length;

            return Task.FromResult(result);
        }

        private FileMetadata GetFile(FileSystemInfo file)
        {
            return new FileMetadata
            {
                Id = file.FullName,
                Name = Path.GetFileName(file.FullName),
                MimeType = MimeTypes.GetMimeType(file.FullName)
            };
        }
    }
}
