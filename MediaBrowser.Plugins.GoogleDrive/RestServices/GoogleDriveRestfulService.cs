using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive.RestServices
{
    public class GoogleDriveRestfulService : IRestfulService
    {
        private readonly IConfigurationRetriever _configurationRetriever;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IGoogleDriveService _googleDriveService;

        public GoogleDriveRestfulService(IConfigurationRetriever configurationRetriever, IGoogleAuthService googleAuthService, IGoogleDriveService googleDriveService)
        {
            _configurationRetriever = configurationRetriever;
            _googleAuthService = googleAuthService;
            _googleDriveService = googleDriveService;
        }

        public async Task Post(CodeRequest code)
        {
            var googleDriveUser = _configurationRetriever.GetUserConfiguration(code.UserId);

            googleDriveUser.AccessToken = await CreateAccessToken(code.Code, code.RedirectUri, googleDriveUser);
            googleDriveUser.FolderId = await GetOrCreateFolder(googleDriveUser);

            _configurationRetriever.SaveConfiguration();
        }

        private async Task<AccessToken> CreateAccessToken(string code, string redirectUri, GoogleDriveUser googleDriveUser)
        {
            var token = await _googleAuthService.GetToken(code, redirectUri, googleDriveUser.GoogleDriveClientId, googleDriveUser.GoogleDriveClientSecret, CancellationToken.None);

            return new AccessToken
            {
                Token = token.access_token,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.expires_in),
                RefreshToken = token.refresh_token
            };
        }

        private async Task<string> GetOrCreateFolder(GoogleDriveUser googleDriveUser)
        {
            var googleCredentials = new GoogleCredentials
            {
                AccessToken = googleDriveUser.AccessToken,
                ClientId = googleDriveUser.GoogleDriveClientId,
                ClientSecret = googleDriveUser.GoogleDriveClientSecret
            };

            return await _googleDriveService.GetOrCreateFolder(Constants.GoogleDriveFolderName, googleCredentials, CancellationToken.None);
        }
    }
}
