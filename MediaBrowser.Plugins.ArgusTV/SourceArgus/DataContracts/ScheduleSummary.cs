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
using System.Web.Script.Serialization;

namespace ArgusTV.DataContracts
{
	/// <summary>
	/// A light-weight version of a schedule (without the rules).
	/// </summary>
	public partial class ScheduleSummary
	{
        /// <summary>
        /// Create a new instance using the default constructor.
        /// </summary>
        public ScheduleSummary()
        {
            this.ScheduleType = ScheduleType.Recording;
        }

        /// <summary>
        /// The unique ID of the schedule.
        /// </summary>
        public Guid ScheduleId { get; set; }

        /// <summary>
        /// The unique integer ID of the schedule.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the schedule.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The channel-type of this schedule.
        /// </summary>
        public ChannelType ChannelType { get; set; }

        /// <summary>
        /// The type of the schedule.
        /// </summary>
        public ScheduleType ScheduleType { get; set; }

        /// <summary>
        /// The priority of the schedule.
        /// </summary>
        public SchedulePriority SchedulePriority { get; set; }

        /// <summary>
        /// Defines how long to keep this recording before deleting it.
        /// </summary>
        public KeepUntilMode KeepUntilMode { get; set; }

        /// <summary>
        /// Defines how long to keep this recording before deleting it (see KeepUntilMode).
        /// </summary>
        public int? KeepUntilValue { get; set; }

        /// <summary>
        /// The number of seconds to start the recording before the program start time, or null for the default.
        /// </summary>
        public int? PreRecordSeconds { get; set; }

        /// <summary>
        /// The number of seconds to stop the recording past the program stop time, or null for the default.
        /// </summary>
        public int? PostRecordSeconds { get; set; }

        /// <summary>
        /// Is this schedule active?
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Is this a one-time schedule?
        /// </summary>
        public bool IsOneTime { get; set; }

        /// <summary>
        /// The time the record was last modified.
        /// </summary>
        public DateTime LastModifiedTime { get; set; }

        /// <summary>
        /// INTERNAL USE ONLY.
        /// </summary>
        public int Version { get; set; }

        #region Miscellanous

        /// <summary>
        /// The number of minutes to start the recording before the program start time, or null for the default.
        /// </summary>
        [ScriptIgnore]
        public double? PreRecordMinutes
        {
            get { return this.PreRecordSeconds / 60.0; }
            set
            {
                this.PreRecordSeconds = value.HasValue ? (int?)Math.Round(value.Value * 60) : null;
            }
        }

        /// <summary>
        /// The number of minutes to stop the recording past the program stop time, or null for the default.
        /// </summary>
        [ScriptIgnore]
        public double? PostRecordMinutes
        {
            get { return this.PostRecordSeconds / 60.0; }
            set
            {
                this.PostRecordSeconds = value.HasValue ? (int?)Math.Round(value.Value * 60) : null;
            }
        }

        #endregion
    }
}
