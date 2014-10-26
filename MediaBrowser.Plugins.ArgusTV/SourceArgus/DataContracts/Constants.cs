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
using System.Collections.Generic;
using System.Text;

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// Global constants for ARGUS TV.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The API version of the main REST services.
        /// </summary>
        public const int RestApiVersion = 67;

        /// <summary>
        /// The minimal API version of the main services still supported for older REST clients.
        /// </summary>
        public const int MinimalSupportedRestApiVersion = 45;

        /// <summary>
        /// The API version of the recorders.
        /// </summary>
        public const int RecorderApiVersion = 1;

        /// <summary>
        /// The assembly version of all main assemblies.
        /// </summary>
        public const string AssemblyVersion = "2.3.0.0";

        /// <summary>
        /// The version of ARGUS TV.
        /// </summary>
        public const string ProductVersion = "2.3 RC";
    }
}
