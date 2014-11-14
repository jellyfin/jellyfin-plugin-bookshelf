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
    /// Minimal information about an upcoming program that is based on guide data.
    /// </summary>
    public partial class UpcomingGuideProgram
	{
        /// <summary>
        /// The channel ID of the upcoming program.
        /// </summary>
        public Guid ChannelId { get; set; }

        /// <summary>
        /// The channel integer ID of the upcoming program.
        /// </summary>
        public int ChannelIntId { get; set; }

        /// <summary>
        /// The schedule ID of the upcoming program.
        /// </summary>
        public Guid ScheduleId { get; set; }

        /// <summary>
        /// The schedule integer ID of the upcoming program.
        /// </summary>
        public int ScheduleIntId { get; set; }

        /// <summary>
        /// The priority of the upcoming program.
        /// </summary>
        public UpcomingProgramPriority Priority { get; set; }

        /// <summary>
        /// Is the upcoming program part of a series?
        /// </summary>
        public bool IsPartOfSeries { get; set; }

        /// <summary>
        /// Is the upcoming program cancelled?
        /// </summary>
        public bool IsCancelled { get; set; }

        /// <summary>
        /// The reason the upcoming program was flagged as cancelled.
        /// </summary>
        public UpcomingCancellationReason CancellationReason { get; set; }

        /// <summary>
        /// The guide program ID of the upcoming program.
        /// </summary>
        public Guid GuideProgramId { get; set; }

        /// <summary>
        /// The guide program integer ID of the upcoming program.
        /// </summary>
        public int GuideProgramIntId { get; set; }

        #region Miscellaneous

        /// <summary>
        /// Create a unique upcoming program ID for a guide program that is scheduled on
        /// a specific channel.
        /// </summary>
        /// <returns>The unique upcoming program ID.</returns>
        public Guid GetUniqueUpcomingProgramId()
        {
            return UpcomingProgram.GetUniqueUpcomingProgramId(this.GuideProgramId, this.ChannelId);
        }

        #endregion
    }
}
