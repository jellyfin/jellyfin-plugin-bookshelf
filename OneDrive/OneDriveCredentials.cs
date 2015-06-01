using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Sync;
using OneDrive.Api;
using OneDrive.Configuration;

namespace OneDrive
{
    public class OneDriveCredentials
    {
        private readonly IConfigurationRetriever _configurationRetriever;
        private readonly ILiveAuthenticationApi _liveAuthenticationApi;
        private readonly string _syncAccountId;
        private readonly Token _accessToken;

        public OneDriveCredentials(IConfigurationRetriever configurationRetriever, ILiveAuthenticationApi liveAuthenticationApi, SyncTarget target)
        {
            _configurationRetriever = configurationRetriever;
            _liveAuthenticationApi = liveAuthenticationApi;
            _syncAccountId = target.Id;

            var syncAccount = configurationRetriever.GetSyncAccount(target.Id);
            _accessToken = syncAccount.AccessToken;
        }

        public async Task<string> GetAccessToken(CancellationToken cancellationToken)
        {
            // Give a buffer around the expiration time
            if (_accessToken.ExpiresAt <= DateTime.UtcNow.AddSeconds(-20))
            {
                await RefreshToken(cancellationToken);
            }

            return _accessToken.AccessToken;
        }

        private async Task RefreshToken(CancellationToken cancellationToken)
        {
            var config = _configurationRetriever.GetGeneralConfiguration();
            var now = DateTime.UtcNow;
            var refreshToken = await _liveAuthenticationApi.RefreshToken(_accessToken.RefresToken, Constants.OneDriveRedirectUrl, config.OneDriveClientId, config.OneDriveClientSecret, cancellationToken);

            _accessToken.AccessToken = refreshToken.access_token;
            _accessToken.ExpiresAt = now.AddSeconds(refreshToken.expires_in);
            _accessToken.RefresToken = refreshToken.refresh_token;

            var syncAccount = _configurationRetriever.GetSyncAccount(_syncAccountId);
            syncAccount.AccessToken = _accessToken;

            _configurationRetriever.AddSyncAccount(syncAccount);
        }
    }
}
