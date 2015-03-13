using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Sync;
using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class GoogleDriveServerSyncProvider : IServerSyncProvider
    {
        private readonly IConfigurationRetriever _configurationRetriever;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly IUserManager _userManager;

        public GoogleDriveServerSyncProvider(IConfigurationRetriever configurationRetriever, IGoogleDriveService googleDriveService, IUserManager userManager)
        {
            _configurationRetriever = configurationRetriever;
            _googleDriveService = googleDriveService;
            _userManager = userManager;
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

            await _googleDriveService.UploadFile(stream, file, googleCredentials, progress, cancellationToken);
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
            if (string.IsNullOrEmpty(user.MediaBrowserUserId))
            {
                return new SyncTarget
                {
                    Id = "single",
                    Name = "Google Drive"
                };
            }

            var mediaBrowserUser = _userManager.GetUserById(user.MediaBrowserUserId);
            return new SyncTarget
            {
                Id = user.MediaBrowserUserId,
                Name = "Google Drive for " + mediaBrowserUser.Name
            };
        }

        private GoogleCredentials GetGoogleCredentials(SyncTarget target)
        {
            var googleDriveUser = _configurationRetriever.GetUserConfiguration(target.Id);

            return new GoogleCredentials
            {
                AccessToken = googleDriveUser.AccessToken,
                ClientId = googleDriveUser.GoogleDriveClientId,
                ClientSecret = googleDriveUser.GoogleDriveClientSecret
            };
        }

        private static GoogleDriveFile CreateGoogleDriveFile(string path)
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
