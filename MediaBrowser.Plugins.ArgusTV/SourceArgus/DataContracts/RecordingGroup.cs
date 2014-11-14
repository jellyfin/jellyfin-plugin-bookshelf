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
    /// A group of recorded programs, grouped by the RecordingGroupMode.
    /// </summary>
    public partial class RecordingGroup
    {
        /// <summary>
        /// INTERNAL USE ONLY.
        /// </summary>
        public RecordingGroup()
        {
        }

        /// <summary>
        /// Constructs a RecordingGroup instance.
        /// </summary>
        /// <param name="recordingGroupMode">The recording group-mode.</param>
        public RecordingGroup(RecordingGroupMode recordingGroupMode)
        {
            this.RecordingGroupMode = recordingGroupMode;
        }

        /// <summary>
        /// The recording group-mode.
        /// </summary>
        public RecordingGroupMode RecordingGroupMode { get; set; }

        /// <summary>
        /// The original schedule ID (may no longer exist).
        /// </summary>
        public Guid ScheduleId { get; set; }

        /// <summary>
        /// The original schedule name.
        /// </summary>
        public string ScheduleName { get; set; }

        /// <summary>
        /// The original schedule priority.
        /// </summary>
        public SchedulePriority SchedulePriority { get; set; }

        /// <summary>
        /// The start time of the most recent recording.
        /// </summary>
        public DateTime LatestProgramStartTime { get; set; }

        /// <summary>
        /// The original channel ID.
        /// </summary>
        public Guid ChannelId { get; set; }

        /// <summary>
        /// The original channel's display name.
        /// </summary>
        public string ChannelDisplayName { get; set; }

        /// <summary>
        /// The original channel's type.
        /// </summary>
        public ChannelType ChannelType { get; set; }

        /// <summary>
        /// The title of the recorded program.
        /// </summary>
        public string ProgramTitle { get; set; }

        /// <summary>
        /// The category of the recorded program.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The number of recordings in the group.
        /// </summary>
        public int RecordingsCount { get; set; }

        /// <summary>
        /// Set to true when at least one program in the group is still recording.
        /// </summary>
        public bool IsRecording { get; set; }

        /// <summary>
        /// Optionally set, and only when the group only contains one recording.
        /// </summary>
        public RecordingSummary SingleRecording { get; set; }
    }
}
