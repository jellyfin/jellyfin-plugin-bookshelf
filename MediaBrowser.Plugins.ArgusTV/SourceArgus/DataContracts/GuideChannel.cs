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
using System.Collections;

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// A guide channel.
    /// </summary>
    public partial class GuideChannel
    {
        /// <summary>
        /// The unique guide channel ID.
        /// </summary>
        public Guid GuideChannelId { get; set; }

        /// <summary>
        /// The channel's XMLTV ID (if known).
        /// </summary>
        public string XmlTvId { get; set; }

        /// <summary>
        /// The name of the guide channel.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the channel.
        /// </summary>
        public ChannelType ChannelType { get; set; }

        /// <summary>
        /// INTERNAL USE ONLY.
        /// </summary>
        public int Version { get; set; }
    }
}
