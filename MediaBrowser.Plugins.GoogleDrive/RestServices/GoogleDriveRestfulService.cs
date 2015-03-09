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

        public GoogleDriveRestfulService(IConfigurationRetriever configurationRetriever, IGoogleAuthService googleAuthService)
        {
            _configurationRetriever = configurationRetriever;
            _googleAuthService = googleAuthService;
        }

        public async Task Post(CodeRequest code)
        {
            var googleDriveUser = _configurationRetriever.GetUserConfiguration(code.UserId);
            var token = await _googleAuthService.GetToken(code.Code, code.RedirectUri, googleDriveUser.GoogleDriveClientId, googleDriveUser.GoogleDriveClientSecret, CancellationToken.None);

            googleDriveUser.AccessToken = CreateAccessToken(token);
            _configurationRetriever.SaveConfiguration();
        }

        private AccessToken CreateAccessToken(AuthorizationAccessToken token)
        {
            return new AccessToken
            {
                Token = token.AccessToken,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.ExpiresIn),
                RefreshToken = token.RefreshToken
            };
        }
    }
}
