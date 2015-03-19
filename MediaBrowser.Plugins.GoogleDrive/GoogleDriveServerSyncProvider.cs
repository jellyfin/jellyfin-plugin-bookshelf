using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Sync;
using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class GoogleDriveServerSyncProvider : IServerSyncProvider
    {
        private readonly IConfigurationRetriever _configurationRetriever;
        private readonly IGoogleDriveService _googleDriveService;

        public GoogleDriveServerSyncProvider(IConfigurationRetriever configurationRetriever, IGoogleDriveService googleDriveService)
        {
            _configurationRetriever = configurationRetriever;
            _googleDriveService = googleDriveService;
        }

        public string Name
        {
            get { return Constants.Name; }
        }

        public IEnumerable<SyncTarget> GetAllSyncTargets()
        {
            return _configurationRetriever.GetSyncAccounts().Select(CreateSyncTarget);
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            return _configurationRetriever.GetUserSyncAccounts(userId).Select(CreateSyncTarget);
        }

        public async Task<SendFileResult> SendFile(Stream stream, string remotePath, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var file = CreateGoogleDriveFile(remotePath, target);
            var googleCredentials = GetGoogleCredentials(target);

            var url = await _googleDriveService.UploadFile(stream, file, googleCredentials, progress, cancellationToken);

            return new SendFileResult
            {
                Path = url,
                Protocol = MediaProtocol.Http
            };
        }

        public async Task DeleteFile(string path, SyncTarget target, CancellationToken cancellationToken)
        {
            var file = CreateGoogleDriveFile(path, target);
            var googleCredentials = GetGoogleCredentials(target);

            await _googleDriveService.DeleteFile(file, googleCredentials, cancellationToken);
        }

        public async Task<Stream> GetFile(string path, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var file = CreateGoogleDriveFile(path, target);
            var googleCredentials = GetGoogleCredentials(target);

            return await _googleDriveService.GetFile(file, googleCredentials, cancellationToken);
        }

        public string GetFullPath(IEnumerable<string> path, SyncTarget target)
        {
            return Path.Combine(path.ToArray());
        }

        public string GetParentDirectoryPath(string path, SyncTarget target)
        {
            return Path.GetDirectoryName(path);
        }

        // Missing CancellationToken
        public async Task<List<DeviceFileInfo>> GetFileSystemEntries(string path, SyncTarget target)
        {
            var googleDriveUser = _configurationRetriever.GetSyncAccount(target.Id);
            var googleCredentials = GetGoogleCredentials(target);

            var files = await _googleDriveService.GetFilesListing(path, googleDriveUser.FolderId, googleCredentials, CancellationToken.None);

            return files.Select(CreateDeviceFileInfo).ToList();
        }

        private SyncTarget CreateSyncTarget(GoogleDriveSyncAccount syncAccount)
        {
            return new SyncTarget
            {
                Id = syncAccount.Id,
                Name = syncAccount.Name
            };
        }

        private GoogleCredentials GetGoogleCredentials(SyncTarget target)
        {
            var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);
            var generalConfig = _configurationRetriever.GetGeneralConfiguration();

            return new GoogleCredentials
            {
                RefreshToken = syncAccount.RefreshToken,
                ClientId = generalConfig.GoogleDriveClientId,
                ClientSecret = generalConfig.GoogleDriveClientSecret
            };
        }

        private GoogleDriveFile CreateGoogleDriveFile(string path, SyncTarget target)
        {
            var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);
            var folder = Path.GetDirectoryName(path);

            return new GoogleDriveFile
            {
                Name = Path.GetFileName(path),
                FolderPath = folder,
                GoogleDriveFolderId = syncAccount.FolderId
            };
        }

        private DeviceFileInfo CreateDeviceFileInfo(GoogleDriveFile file)
        {
            return new DeviceFileInfo
            {
                Name = file.Name,
                Path = GetGoogleDriveFilePath(file)
            };
        }

        private static string GetGoogleDriveFilePath(GoogleDriveFile file)
        {
            if (!string.IsNullOrEmpty(file.FolderPath))
            {
                return Path.Combine(file.FolderPath, file.Name);
            }

            return file.Name;
        }
    }
}
