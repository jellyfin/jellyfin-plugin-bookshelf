namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class GoogleDriveUser
    {
        public string MediaBrowserUserId { get; set; }
        public string GoogleDriveClientId { get; set; }
        public string GoogleDriveClientSecret { get; set; }
        public AccessToken AccessToken { get; set; }
    }
}
