using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Sync;
using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class GoogleDriveCloudSyncProvider : ICloudSyncProvider
    {
        private readonly IConfigurationRetriever _configurationRetriever;
        private readonly IGoogleDriveService _googleDriveService;

        public GoogleDriveCloudSyncProvider(IConfigurationRetriever configurationRetriever, IGoogleDriveService googleDriveService)
        {
            _configurationRetriever = configurationRetriever;
            _googleDriveService = googleDriveService;
        }

        public string Name
        {
            get { return Constants.Name; }
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            var googleDriveUser = _configurationRetriever.GetUserConfiguration(userId);

            if (googleDriveUser != null)
            {
                yield return new SyncTarget
                {
                    Id = userId,
                    Name = Name
                };
            }
        }

        public async Task SendFile(string inputFile, string[] pathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var googleDriveUser = _configurationRetriever.GetUserConfiguration(target.Id);
            await _googleDriveService.Upload("filename", inputFile, googleDriveUser, cancellationToken);
        }

        public Task<Stream> GetFile(string[] pathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
