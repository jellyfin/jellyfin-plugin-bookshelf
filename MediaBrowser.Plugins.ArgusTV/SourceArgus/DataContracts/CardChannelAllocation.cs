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

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// Contains information about a channel that is allocated to a recorder's card.
    /// </summary>
	public partial class CardChannelAllocation
	{
        /// <summary>
        /// The ID of the recorder used for this recording.
        /// </summary>
        public Guid RecorderTunerId { get; set; }

        /// <summary>
        /// The unique ID of the recorder's card that will be used for the recording.
        /// </summary>
        public string CardId { get; set; }

        /// <summary>
        /// The type of the channel that will be recorded.
        /// </summary>
        public ChannelType ChannelType { get; set; }

        /// <summary>
        /// The ID of the channel that will be recorded.
        /// </summary>
        public Guid ChannelId { get; set; }

        /// <summary>
        /// The name of the channel that will be recorded.
        /// </summary>
        public string ChannelName { get; set; }
	}
}
