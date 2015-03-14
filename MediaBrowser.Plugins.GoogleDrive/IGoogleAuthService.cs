using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public interface IGoogleAuthService
    {
        Task<AuthorizationAccessToken> GetToken(string code, string redirectUri, string clientId, string clientSecret, CancellationToken cancellationToken);
    }
}
