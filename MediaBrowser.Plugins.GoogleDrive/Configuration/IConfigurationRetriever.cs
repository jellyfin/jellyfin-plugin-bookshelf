namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public interface IConfigurationRetriever
    {
        GoogleDriveUser GetUserConfiguration(string userId);
        void SaveConfiguration();
    }
}
