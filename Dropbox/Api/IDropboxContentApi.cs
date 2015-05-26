using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dropbox.Api
{
    public interface IDropboxContentApi
    {
        Task<ChunkedUploadResult> ChunkedUpload(string uploadId, byte[] content, int offset, string accessToken, CancellationToken cancellationToken);
        Task CommitChunkedUpload(string path, string uploadId, string accessToken, CancellationToken cancellationToken);
        Task<Stream> Files(string path, string accessToken, CancellationToken cancellationToken);
    }
}
