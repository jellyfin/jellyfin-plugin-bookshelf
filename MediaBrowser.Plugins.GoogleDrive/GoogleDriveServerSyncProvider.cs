using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Sync;
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
            return _configurationRetriever.GetConfigurations().Select(CreateSyncTarget);
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            var googleDriveUser = _configurationRetriever.GetUserConfiguration(userId);

            if (googleDriveUser != null)
            {
                yield return CreateSyncTarget(googleDriveUser);
            }
        }

        public async Task SendFile(Stream stream, string remotePath, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var file = CreateGoogleDriveFile(remotePath);
            var googleCredentials = GetGoogleCredentials(target);

            await _googleDriveService.UploadFile(stream, file, googleCredentials, cancellationToken);
        }

        public async Task DeleteFile(string path, SyncTarget target, CancellationToken cancellationToken)
        {
            var file = CreateGoogleDriveFile(path);
            var googleCredentials = GetGoogleCredentials(target);

            await _googleDriveService.DeleteFile(file, googleCredentials, cancellationToken);
        }

        public async Task<Stream> GetFile(string path, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var file = CreateGoogleDriveFile(path);
            var googleCredentials = GetGoogleCredentials(target);

            return await _googleDriveService.GetFile(file, googleCredentials, cancellationToken);
        }

        // string is not very flexible...
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
            var googleCredentials = GetGoogleCredentials(target);

            var files = await _googleDriveService.GetFilesListing(path, googleCredentials, CancellationToken.None);

            return files.Select(CreateDeviceFileInfo).ToList();
        }

        private SyncTarget CreateSyncTarget(GoogleDriveUser user)
        {
            return new SyncTarget
            {
                Id = user.Id,
                Name = user.Name
            };
        }

        private GoogleCredentials GetGoogleCredentials(SyncTarget target)
        {
            var googleDriveUser = _configurationRetriever.GetUserConfigurationById(target.Id);

            return new GoogleCredentials
            {
                AccessToken = googleDriveUser.AccessToken,
                ClientId = googleDriveUser.GoogleDriveClientId,
                ClientSecret = googleDriveUser.GoogleDriveClientSecret
            };
        }

        private GoogleDriveFile CreateGoogleDriveFile(string path)
        {
            var folder = Path.GetDirectoryName(path);

            return new GoogleDriveFile
            {
                Name = Path.GetFileName(path),
                FolderPath = folder
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

        private string GetGoogleDriveFilePath(GoogleDriveFile file)
        {
            if (!string.IsNullOrEmpty(file.FolderPath))
            {
                return Path.Combine(file.FolderPath, file.Name);
            }

            return file.Name;
        }
    }
}
