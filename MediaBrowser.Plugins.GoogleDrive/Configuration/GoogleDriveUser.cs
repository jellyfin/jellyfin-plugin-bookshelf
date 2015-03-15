namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class GoogleDriveUser
    {
        public string MediaBrowserUserId { get; set; }
        public AccessToken AccessToken { get; set; }
        public string FolderId { get; set; }
    }
}
