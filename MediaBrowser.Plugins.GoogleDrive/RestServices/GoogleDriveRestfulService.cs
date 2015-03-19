using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Controller.Net;
using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive.RestServices
{
    [Authenticated]
    public class GoogleDriveRestfulService2 : IRestfulService
    {
        private readonly IConfigurationRetriever _configurationRetriever;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IGoogleDriveService _googleDriveService;

        public GoogleDriveRestfulService2(IConfigurationRetriever configurationRetriever, IGoogleAuthService googleAuthService, IGoogleDriveService googleDriveService)
        {
            _configurationRetriever = configurationRetriever;
            _googleAuthService = googleAuthService;
            _googleDriveService = googleDriveService;
        }

        public void Delete(DeleteSyncTarget request)
        {
            _configurationRetriever.RemoveSyncAccount(request.Id);
        }

        public async Task Post(AddSyncTarget request)
        {
            var config = _configurationRetriever.GetGeneralConfiguration();
            var refreshToken = await GetRefreshToken(request);

            var syncAccount = new GoogleDriveSyncAccount
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                EnableForEveryone = request.EnableForEveryone,
                UserIds = request.UserIds,
                RefreshToken = refreshToken,
                FolderId = await GetOrCreateFolder(config.GoogleDriveClientId, config.GoogleDriveClientSecret, refreshToken)
            };

            if (!string.IsNullOrEmpty(request.Id))
            {
                syncAccount.Id = request.Id;
            }

            _configurationRetriever.AddSyncAccount(syncAccount);
        }

        public GoogleDriveSyncAccount Get(GetSyncTarget request)
        {
            return _configurationRetriever.GetSyncAccount(request.Id);
        }

        private async Task<string> GetRefreshToken(AddSyncTarget request)
        {
            var config = _configurationRetriever.GetGeneralConfiguration();
            var redirectUri = HttpUtility.UrlDecode(request.RedirectUri);

            var token = await _googleAuthService.GetToken(request.Code, redirectUri, config.GoogleDriveClientId, config.GoogleDriveClientSecret, CancellationToken.None);
            return token.refresh_token;
        }

        private async Task<string> GetOrCreateFolder(string clientId, string clientSecret, string refreshToken)
        {
            var googleCredentials = new GoogleCredentials
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RefreshToken = refreshToken
            };

            return await _googleDriveService.GetOrCreateFolder(Constants.GoogleDriveFolderName, googleCredentials, CancellationToken.None);
        }
    }
}
