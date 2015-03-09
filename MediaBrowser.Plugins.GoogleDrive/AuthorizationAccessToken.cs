namespace MediaBrowser.Plugins.GoogleDrive
{
    public class AuthorizationAccessToken
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; }
    }
}
