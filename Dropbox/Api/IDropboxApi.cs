using System.Threading;
using System.Threading.Tasks;

namespace Dropbox.Api
{
    public interface IDropboxApi
    {
        Task<AuthorizationToken> AcquireToken(string code, string appKey, string appSecret, CancellationToken cancellationToken);
        Task<MetadataResult> Metadata(string path, string accessToken, CancellationToken cancellationToken);
        Task Delete(string path, string accessToken, CancellationToken cancellationToken);
        Task<MediaResult> Media(string path, string accessToken, CancellationToken cancellationToken);
        Task<DeltaResult> Delta(string cursor, string accessToken, CancellationToken cancellationToken);
    }
}
