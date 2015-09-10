using System.Threading;
using System.Threading.Tasks;

namespace OneDrive.Api
{
    public interface ILiveAuthenticationApi
    {
        Task<AuthorizationToken> AcquireToken(string code, string redirectUrl, string clientId, string clientSecret, CancellationToken cancellationToken);
        Task<AuthorizationToken> RefreshToken(string refreshToken, string redirectUrl, string clientId, string clientSecret, CancellationToken cancellationToken);
    }
}
