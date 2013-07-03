using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.MpcHc
{
    public static class MpcHcService
    {
        /// <summary>
        /// Gets the server name that the http interface will be running on
        /// </summary>
        /// <value>The HTTP server.</value>
        public static string HttpServer
        {
            get
            {
                return "localhost";
            }
        }

        /// <summary>
        /// Gets the port that the web interface will be running on
        /// </summary>
        /// <value>The HTTP port.</value>
        public static string HttpPort
        {
            get
            {
                return "13579";
            }
        }

        /// <summary>
        /// Gets the url of that will be called to for status
        /// </summary>
        /// <value>The status URL.</value>
        public static string StatusUrl
        {
            get
            {
                return "http://" + HttpServer + ":" + HttpPort + "/status.html";
            }
        }

        /// <summary>
        /// Gets the url of that will be called to send commands
        /// </summary>
        /// <value>The command URL.</value>
        public static string CommandUrl
        {
            get
            {
                return "http://" + HttpServer + ":" + HttpPort + "/command.html";
            }
        }
    }
}
