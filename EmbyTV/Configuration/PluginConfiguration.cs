using EmbyTV.TunerHost.Settings;
using MediaBrowser.Model.Plugins;

namespace EmbyTV.Configuration
{
    public class PluginConfiguration:BasePluginConfiguration
    {
        public string apiURL { get; set; }
        public string Port { get; set; }
        public bool loadOnlyFavorites { get; set; }
        public string hashPassword { get; set; }
        public string username { get; set; }
        public string tvLineUp { get; set; }
        public string avaliableLineups{get;set;}
        public string headendName { get; set; }
        public string headendValue { get; set; }
        public string zipCode { get; set; }
        public TunerHostSettings Settings { get; set; }

        public PluginConfiguration()
        {
            Port = "5004";
            apiURL = "localhost";
            loadOnlyFavorites = true;
            tvLineUp = "";
            username = "";
            hashPassword = "";
            avaliableLineups = "";
            headendName = "";
            headendValue = "";
            zipCode = "";
            Settings = new TunerHostSettings()
            {
                Settings = new Constructor[]
                {
                    new Constructor() { Name="hostname",Label = "HomeRun hostname or IP address:", DefaultValue = "localhost", Type = "Single", Description = "Tunner url (format --> {hostname})." },
                    new Constructor() { Name = "Test Name 2", DefaultValue = "Default Value 2", Type = "Single 2", Description = "test config generator 2" }
                }
            };
        }
    }
}
