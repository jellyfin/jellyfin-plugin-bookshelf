using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Drive.v2;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Sync;
using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class GoogleDriveCloudSyncProvider : ICloudSyncProvider
    {
        private readonly IConfigurationRetriever _configurationRetriever;

        public GoogleDriveCloudSyncProvider(IConfigurationRetriever configurationRetriever)
        {
            _configurationRetriever = configurationRetriever;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return Constants.Name; }
        }

        /// <summary>
        /// Gets the synchronize targets.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>IEnumerable&lt;SyncTarget&gt;.</returns>
        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            var googleDriveUser = _configurationRetriever.GetUserConfiguration(userId);

            if (googleDriveUser != null)
            {
                yield return new SyncTarget
                {
                    Id = googleDriveUser.Token,
                    Name = Name
                };
            }
        }

        /// <summary>
        /// Transfers the item file.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="inputFile">The input file.</param>
        /// <param name="pathParts">The path parts.</param>
        /// <param name="target">The target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task TransferItemFile(string serverId, string itemId, string inputFile, string[] pathParts, SyncTarget target, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
