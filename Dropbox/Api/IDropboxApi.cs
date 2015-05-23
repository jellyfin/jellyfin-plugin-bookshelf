using System.Threading;
using System.Threading.Tasks;

namespace Dropbox.Api
{
    public interface IDropboxApi
    {
        Task<AuthorizationToken> AcquireToken(string code, string appKey, string appSecret, CancellationToken cancellationToken);
        Task<MetadataResult> Metadata(string path, string accessToken, CancellationToken cancellationToken);
        Task Delete(string path, string accessToken, CancellationToken cancellationToken);
        Task<ShareResult> Shares(string path, string accessToken, CancellationToken cancellationToken);
    }
}
