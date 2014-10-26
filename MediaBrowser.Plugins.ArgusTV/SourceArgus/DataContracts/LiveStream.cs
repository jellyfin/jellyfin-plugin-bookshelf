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

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// A live stream (rtsp).
    /// </summary>
    public partial class LiveStream
	{
        /// <summary>
        /// Create a new instance using the default constructor.
        /// </summary>
        public LiveStream()
        {
        }

        /// <summary>
        /// Create a new instance of LiveStream.
        /// </summary>
        /// <param name="channel">The channel being streamed.</param>
        /// <param name="rtspUrl">The rtsp URL to the stream.</param>
        public LiveStream(Channel channel, string rtspUrl)
            : this(channel, rtspUrl, DateTime.UtcNow)
        {
        }

        /// <summary>
        /// Create a new instance of LiveStream.
        /// </summary>
        /// <param name="channel">The channel being streamed.</param>
        /// <param name="rtspUrl">The rtsp URL to the stream.</param>
        /// <param name="streamStartedTimeUtc">The date and time the stream was first started.</param>
        public LiveStream(Channel channel, string rtspUrl, DateTime streamStartedTimeUtc)
        {
            this.Channel = channel;
            this.RtspUrl = rtspUrl;
            this.StreamStartedTime = streamStartedTimeUtc.ToLocalTime();
            this.StreamLastAliveTimeUtc = streamStartedTimeUtc;
        }

        /// <summary>
        /// The channel being streamed.
        /// </summary>
        public Channel Channel { get; set; }

        /// <summary>
        /// The ID of the recorder this stream is running on.
        /// </summary>
        public Guid RecorderTunerId { get; set; }

        /// <summary>
        /// The unique ID of the recorder's card that is being used for the streaming (if provided by the recorder).
        /// </summary>
        public string CardId { get; set; }

        /// <summary>
        /// The rtsp URL to the stream.
        /// </summary>
        public string RtspUrl { get; set; }

        /// <summary>
        /// The UNC path to a timeshift file of the live stream (if available).
        /// </summary>
        public string TimeshiftFile { get; set; }

        /// <summary>
        /// The date and time the stream was first started.
        /// </summary>
        public DateTime StreamStartedTime { get; set; }

        /// <summary>
        /// The date and time the stream was last sent a keep-alive signal (UTC).
        /// </summary>
        public DateTime StreamLastAliveTimeUtc { get; set; }
    }
}
