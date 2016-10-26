using System;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Services;
using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive.RestServices
{
    [Authenticated]
    public class GoogleDriveRestfulService2 : IService
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
                Name = WebUtility.UrlDecode(request.Name),
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

        public string Get(UrlEncodeRequest request)
        {
            var encoded = WebUtility.UrlEncode(request.Str);

            return Regex.Replace(encoded, @"%[a-f0-9]{2}", m => m.Value.ToUpperInvariant());
        }

        private async Task<string> GetRefreshToken(AddSyncTarget request)
        {
            var config = _configurationRetriever.GetGeneralConfiguration();
            var redirectUri = request.RedirectUri;

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

            return await _googleDriveService.GetOrCreateFolder(Constants.GoogleDriveFolderName, null, googleCredentials, CancellationToken.None);
        }
    }
}
