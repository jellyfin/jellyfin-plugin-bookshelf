using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class GoogleDriveUserDto
    {
        public string GoogleDriveClientId { get; set; }
        public string GoogleDriveClientSecret { get; set; }
        public GoogleDriveUser User { get; set; }
    }
}
