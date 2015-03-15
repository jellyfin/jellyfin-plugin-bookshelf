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

            var accessToken = await CreateAccessToken(code.Code, code.RedirectUri, googleDriveUser);

            _configurationRetriever.SaveUserConfiguration(code.UserId, accessToken, googleDriveUser.User.FolderId);

            var folderId = await GetOrCreateFolder(googleDriveUser);

            _configurationRetriever.SaveUserConfiguration(code.UserId, accessToken, folderId);
        }

        private async Task<AccessToken> CreateAccessToken(string code, string redirectUri, GoogleDriveUserDto googleDriveUser)
        {
            var token = await _googleAuthService.GetToken(code, redirectUri, googleDriveUser.GoogleDriveClientId, googleDriveUser.GoogleDriveClientSecret, CancellationToken.None);

            return new AccessToken
            {
                Token = token.access_token,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.expires_in),
                RefreshToken = token.refresh_token
            };
        }

        private async Task<string> GetOrCreateFolder(GoogleDriveUserDto googleDriveUser)
        {
            var googleCredentials = new GoogleCredentials
            {
                AccessToken = googleDriveUser.User.AccessToken,
                ClientId = googleDriveUser.GoogleDriveClientId,
                ClientSecret = googleDriveUser.GoogleDriveClientSecret
            };

            return await _googleDriveService.GetOrCreateFolder(Constants.GoogleDriveFolderName, googleCredentials, CancellationToken.None);
        }
    }
}
