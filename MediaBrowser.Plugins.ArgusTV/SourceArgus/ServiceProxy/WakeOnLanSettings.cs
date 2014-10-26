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

namespace ArgusTV.ServiceProxy
{
    /// <summary>
    /// Settings to handle wake-on-lan.
    /// </summary>
    public class WakeOnLanSettings
    {
        /// <summary>
        /// The default wake-on-lan timeout.
        /// </summary>
        public const int DefaultTimeoutSeconds = 60;

        /// <summary>
        /// Default settings constructor.
        /// </summary>
        public WakeOnLanSettings()
        {
            this.MacAddresses = String.Empty;
            this.IPAddress = String.Empty;
            this.TimeoutSeconds = DefaultTimeoutSeconds;
        }

        /// <summary>
        /// Is wake-on-lan enabled to wake up the server if needed?  Will only have effect if the
        /// server's MAC address(es) are set.
        /// </summary>
        public bool Enabled { set; get; }

        /// <summary>
        /// The MAC address(es) of the server, separated by a ; character. This property will always
        /// be set after calling ServiceChannelFactories.Initialize(). Store this on the client to
        /// support wake-on-lan for future connections to the server.
        /// </summary>
        public string MacAddresses { set; get; }

        /// <summary>
        /// The IP address of the server. This property will always be set after calling
        /// ServiceChannelFactories.Initialize(). Store this on the client to support wake-on-lan
        /// for future connections to the server.
        /// </summary>
        public string IPAddress { set; get; }

        /// <summary>
        /// The maximum time in seconds to wait for the server to wake up, if wake-on-lan is turned on.
        /// </summary>
        public int TimeoutSeconds { set; get; }
    }
}
