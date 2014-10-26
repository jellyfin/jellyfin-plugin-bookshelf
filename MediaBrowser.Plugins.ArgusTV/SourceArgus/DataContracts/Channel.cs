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
using System.Globalization;

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// A TV or radio channel.
    /// </summary>
    public partial class Channel
    {
        /// <summary>
        /// Create a new instance using the default constructor.
        /// </summary>
        public Channel()
        {
            this.ChannelType = DataContracts.ChannelType.Television;
        }

        /// <summary>
        /// The unique channel ID.
        /// </summary>
        public Guid ChannelId { get; set; }

        /// <summary>
        /// The unique integer ID of the channel.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ID of the guide channel that holds the guide data for this channel.
        /// </summary>
        public Guid? GuideChannelId { get; set; }

        /// <summary>
        /// The type of the channel.
        /// </summary>
        public ChannelType ChannelType { get; set; }

        /// <summary>
        /// The display name of the channel.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The logical channel number of the channel (null if not set).
        /// </summary>
        public int? LogicalChannelNumber { get; set; }

        /// <summary>
        /// Is this channel visible in the guide?
        /// </summary>
        public bool VisibleInGuide { get; set; }

        /// <summary>
        /// The number of default pre-record seconds to use for new schedules on this channel, or null to use the global default.
        /// </summary>
        public int? DefaultPreRecordSeconds { get; set; }

        /// <summary>
        /// The number of default post-record seconds to use for new schedules on this channel, or null to use the global default.
        /// </summary>
        public int? DefaultPostRecordSeconds { get; set; }

        /// <summary>
        /// In case a channel shares its broadcast with another channel you can set its broadcast
        /// start time to make sure recordings are never started when the channel is not available yet.
        /// </summary>
        public string BroadcastStart { get; set; }

        /// <summary>
        /// In case a channel shares its broadcast with another channel you can set its broadcast
        /// stop time to make sure recordings are never extended beyond the time the channel is available.
        /// </summary>
        public string BroadcastStop { get; set; }

        /// <summary>
        /// The sequence number by which channels are ordered.
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// INTERNAL USE ONLY.
        /// </summary>
        public int Version { get; set; }

        #region Miscellanous

        /// <summary>
        /// Channel name prefixed with the logical channel number (if there is one).
        /// </summary>
        public string CombinedDisplayName
        {
            get
            {
                if (this.LogicalChannelNumber.HasValue)
                {
                    return String.Format(CultureInfo.CurrentCulture, "{0} - {1}", this.LogicalChannelNumber.Value, this.DisplayName);
                }
                return this.DisplayName;
            }
        }

        #endregion
    }
}
