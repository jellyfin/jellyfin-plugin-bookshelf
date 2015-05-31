using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using OneDrive.Api;
using OneDrive.Configuration;

namespace OneDrive.RestServices
{
    [Authenticated]
    public class OneDriveRestfulService : IRestfulService
    {
        private readonly IConfigurationRetriever _configurationRetriever;
        private readonly ILiveAuthenticationApi _liveAuthenticationApi;

        public OneDriveRestfulService(IConfigurationRetriever configurationRetriever, ILiveAuthenticationApi liveAuthenticationApi)
        {
            _configurationRetriever = configurationRetriever;
            _liveAuthenticationApi = liveAuthenticationApi;
        }

        public void Delete(DeleteSyncTarget request)
        {
            _configurationRetriever.RemoveSyncAccount(request.Id);
        }

        public async Task Post(AddSyncTarget request)
        {
            var accessToken = await GetAccessToken(request.Code);

            var syncAccount = new OneDriveSyncAccount
            {
                Id = Guid.NewGuid().ToString(),
                Name = WebUtility.UrlDecode(request.Name),
                EnableForEveryone = request.EnableForEveryone,
                UserIds = request.UserIds,
                AccessToken = accessToken
            };

            if (!string.IsNullOrEmpty(request.Id))
            {
                syncAccount.Id = request.Id;
            }

            _configurationRetriever.AddSyncAccount(syncAccount);
        }

        public OneDriveSyncAccount Get(GetSyncTarget request)
        {
            return _configurationRetriever.GetSyncAccount(request.Id);
        }

        private async Task<Token> GetAccessToken(string code)
        {
            var config = _configurationRetriever.GetGeneralConfiguration();

            var now = DateTime.UtcNow;
            var token = await _liveAuthenticationApi.AcquireToken(code, Constants.OneDriveRedirectUrl, config.OneDriveClientId, config.OneDriveClientSecret, CancellationToken.None);

            return new Token
            {
                AccessToken = token.access_token,
                ExpiresAt = now.AddSeconds(token.expires_in),
                RefresToken = token.refresh_token
            };
        }
    }
}
