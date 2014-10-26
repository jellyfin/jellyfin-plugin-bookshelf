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
    /// A TV or radio channel group.
    /// </summary>
    public partial class ChannelGroup
    {
        /// <summary>
        /// The default constructor.
        /// </summary>
        public ChannelGroup()
        {
            this.ChannelType = ChannelType.Television;
        }

        /// <summary>
        /// Identifier for the special 'All TV Channels' group.
        /// </summary>
        public static readonly Guid AllTvChannelsGroupId = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        /// <summary>
        /// Identifier for the special 'All Radio Channels' group.
        /// </summary>
        public static readonly Guid AllRadioChannelsGroupId = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);

        /// <summary>
        /// The unique ID of the channel group.
        /// </summary>
        public Guid ChannelGroupId { get; set; }

        /// <summary>
        /// The unique integer ID of the channel group.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The type of the channels in the group.
        /// </summary>
        public ChannelType ChannelType { get; set; }

        /// <summary>
        /// The name of the group.
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// Is this group visible in the guide?
        /// </summary>
        public bool VisibleInGuide { get; set; }

        /// <summary>
        /// The sequence number by which groups are ordered.
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// INTERNAL USE ONLY.
        /// </summary>
        public int Version { get; set; }
    }
}
