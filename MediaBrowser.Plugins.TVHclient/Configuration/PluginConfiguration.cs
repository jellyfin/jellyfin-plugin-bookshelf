using System;
using MediaBrowser.Model.Plugins;

namespace TVHeadEnd.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string TVH_ServerName { get; set; }
		public int HTTP_Port { get; set; }
		public int HTSP_Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        //public int ConnectionTimeout { get; set; }
        //public int ResponseTimeout { get; set; }

        public PluginConfiguration()
        {
            TVH_ServerName = "localhost";
            HTTP_Port = 9981;
			HTSP_Port = 9982;
            Username = "";
            Password = "";
            //ConnectionTimeout = 30;
            //ResponseTimeout = 15;
        }
    }
}