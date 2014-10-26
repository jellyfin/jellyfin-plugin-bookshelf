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
    /// The current and the next guide program on a channel.
    /// </summary>
    public partial class CurrentAndNextProgram
	{
        /// <summary>
        /// The channel the programs are on.
        /// </summary>
        public Channel Channel { get; set; }

        /// <summary>
        /// The program currently being broadcast, or null if no program was found.
        /// </summary>
        public GuideProgramSummary Current { get; set; }

        /// <summary>
        /// The next program on the channel, or null if no program was found.
        /// </summary>
        public GuideProgramSummary Next { get; set; }

        /// <summary>
        /// If known, the live tuning state of the channel at the time the information
        /// was requested.
        /// </summary>
        public ChannelLiveState LiveState { get; set; }

        /// <summary>
        /// The percentage of how far the current program has been broadcast at this moment.
        /// </summary>
        public int CurrentPercentageComplete
        {
            get
            {
                if (this.Current != null)
                {
                    TimeSpan duration = this.Current.StopTimeUtc - this.Current.StartTimeUtc;
                    TimeSpan elapsed = DateTime.UtcNow - this.Current.StartTimeUtc;
                    if (elapsed >= duration)
                    {
                        return 100;
                    }
                    return (int)(0.5 + (100 * elapsed.TotalSeconds) / duration.TotalSeconds);
                }
                return 0;
            }
        }
    }
}
