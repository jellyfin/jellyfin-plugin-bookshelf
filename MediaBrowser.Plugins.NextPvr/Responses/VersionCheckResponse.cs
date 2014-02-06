namespace MediaBrowser.Plugins.NextPvr.Responses
{
    public static class VersionCheckResponse
    {
        
        public class VersionCheck
        {
            public bool upgradeAvailable { get; set; }
            public string onlineVer { get; set; }
            public string serverVer { get; set; }
        }

        public class RootObject
        {
            public VersionCheck versionCheck { get; set; }
        }
    }
}
