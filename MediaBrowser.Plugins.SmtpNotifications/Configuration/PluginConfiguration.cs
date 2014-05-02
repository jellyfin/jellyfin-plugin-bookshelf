using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.SmtpNotifications.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public SMTPOptions[] Options { get; set; }

        public PluginConfiguration()
        {
            Options = new SMTPOptions[] { };
        }
    }

    public class SMTPOptions
    {
        public bool Enabled { get; set; }
        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public bool UseCredentials { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string PwData { get; set; }
        public string MediaBrowserUserId { get; set; }
        public bool SSL { get; set; }

        public SMTPOptions()
        {
            Port = 25;
            Enabled = true;
            SSL = false;
        }
    }
}
