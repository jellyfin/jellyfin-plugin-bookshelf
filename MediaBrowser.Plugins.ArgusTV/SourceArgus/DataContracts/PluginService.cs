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
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;

namespace ArgusTV.DataContracts
{
	/// <summary>
	/// A plugin-service. Currently these are all recorders.
	/// </summary>
	public partial class PluginService
	{
        /// <summary>
        /// The unique ID of the plugin service.
        /// </summary>
        public Guid PluginServiceId { get; set; }

        /// <summary>
        /// The name of the plugin service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The relative priority of the plugin service as an integer.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Is the plugin service active?
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The interface name implemented by the plugin service, currently always "Recorder".
        /// </summary>
        public string ServiceInterface { get; set; }

        /// <summary>
        /// The REST URL of the plugin service.
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// If known, the IP address of the machine the plugin service is running on.
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// If known, the MAC addresses of the machine the plugin service is running on.
        /// </summary>
        public string MacAddresses { get; set; }
    }
}
