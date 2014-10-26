/*
 *	Copyright (C) 2007-2014 ARGUS TV
 *	http://www.argus-tv.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */
using System;
using System.Globalization;

namespace ArgusTV.ServiceProxy
{
    /// <summary>
    /// Settings to locate the ARGUS TV Recorder service.
    /// </summary>
    public class ServerSettings : ICloneable
    {
        /// <summary>
        /// The default HTTP port.
        /// </summary>
        public const int DefaultHttpPort = 49943;

        /// <summary>
        /// The default HTTPS port.
        /// </summary>
        public const int DefaultHttpsPort = 49941;

        private const string _defaultServerName = "localhost";
        private const int _defaultPort = DefaultHttpPort;
        private const ServiceTransport _defaultTransport = ServiceTransport.Http;

        /// <summary>
        /// Default settings constructor.
        /// </summary>
        public ServerSettings()
        {
            this.ServerName = _defaultServerName;
            this.Transport = _defaultTransport;
            this.Port = _defaultPort;
        }

        /// <summary>
        /// The service transport protocol to use.
        /// </summary>
        public ServiceTransport Transport { set; get; }

        /// <summary>
        /// The server IP or name where the ARGUS TV Recorder service is located.
        /// </summary>
        public string ServerName { set; get; }

        /// <summary>
        /// The port of the ARGUS TV Recorder service.
        /// </summary>
        public int Port { set; get; }

        /// <summary>
        /// The user name to use for HTTPS access to the ARGUS TV Recorder service.
        /// </summary>
        public string UserName { set; get; }

        /// <summary>
        /// The password to use for HTTPS access to the ARGUS TV Recorder service.
        /// </summary>
        public string Password { set; get; }

        private WakeOnLanSettings _wakeOnLanSettings = new WakeOnLanSettings();

        /// <summary>
        /// The wake-on-lan settings.
        /// </summary>
        public WakeOnLanSettings WakeOnLan
        {
            get { return _wakeOnLanSettings; }
            set { _wakeOnLanSettings = value; }
        }
                
        /// <summary>
        /// Returns the URL prefix of the location of the ARGUS TV Recorder service.
        /// </summary>
        public string ServiceUrlPrefix
        {
            get
            {
                string prefix = String.Empty;
                if (this.Transport == ServiceTransport.Http)
                {
                    prefix = "http";
                }
                else if (this.Transport == ServiceTransport.Https)
                {
                    prefix = "https";
                }
                return String.Format(CultureInfo.InvariantCulture, "{0}://{1}:{2}/ArgusTV/", prefix, this.ServerName, this.Port);
            }
        }

        #region ICloneable Members

        /// <summary>
        /// Clone the server settings.
        /// </summary>
        /// <returns>A new server settings instance.</returns>
        public object Clone()
        {
            ServerSettings serverSettings = new ServerSettings();
            serverSettings.Transport = this.Transport;
            serverSettings.ServerName = this.ServerName;
            serverSettings.Port = this.Port;
            serverSettings.UserName = this.UserName;
            serverSettings.Password = this.Password;
            serverSettings.WakeOnLan.Enabled = this.WakeOnLan.Enabled;
            serverSettings.WakeOnLan.IPAddress = this.WakeOnLan.IPAddress;
            serverSettings.WakeOnLan.MacAddresses = this.WakeOnLan.MacAddresses;
            serverSettings.WakeOnLan.TimeoutSeconds = this.WakeOnLan.TimeoutSeconds;
            return serverSettings;
        }

        #endregion
    }
}
