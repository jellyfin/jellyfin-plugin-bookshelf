namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class GoogleDriveUser
    {
        public string MediaBrowserUserId { get; set; }
        // TODO: set Id and Name somewhere
        public string Id { get; set; }
        public string Name { get; set; }
        public string GoogleDriveClientId { get; set; }
        public string GoogleDriveClientSecret { get; set; }
        public AccessToken AccessToken { get; set; }
    }
}
