using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public interface IGoogleDriveService
    {
        Task Upload(string filename, string inputFile, GoogleDriveUser googleDriveUser, CancellationToken cancellationToken);
    }
}
