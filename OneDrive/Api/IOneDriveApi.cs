using System.Threading;
using System.Threading.Tasks;

namespace OneDrive.Api
{
    public interface IOneDriveApi
    {
        Task<UploadSession> CreateUploadSession(string path, OneDriveCredentials credentials, CancellationToken cancellationToken);
        Task<UploadSession> UploadFragment(string url, long rangeStart, long rangeEnd, long totalLength, byte[] content, OneDriveCredentials credentials, CancellationToken cancellationToken);
        Task CreateFolder(string path, string name, OneDriveCredentials credentials, CancellationToken cancellationToken);
        Task Delete(string id, OneDriveCredentials credentials, CancellationToken cancellationToken);
        Task<LinkResult> CreateLink(string id, OneDriveCredentials credentials, CancellationToken cancellationToken);
        Task<ViewChangesResult> ViewChangeById(string id, OneDriveCredentials credentials, CancellationToken cancellationToken);
        Task<ViewChangesResult> ViewChangeByPath(string path, OneDriveCredentials credentials, CancellationToken cancellationToken);
        Task<ViewChangesResult> ViewChanges(string token, OneDriveCredentials credentials, CancellationToken cancellationToken);
    }
}
