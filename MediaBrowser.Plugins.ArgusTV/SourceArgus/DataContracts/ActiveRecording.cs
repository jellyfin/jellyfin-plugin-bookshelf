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
    /// An active recording.
    /// </summary>
    public partial class ActiveRecording
	{
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ActiveRecording()
        {
            this.ConflictingPrograms = new List<Guid>();
        }

        /// <summary>
        /// The card that was allocated for the required channel.
        /// </summary>
        public CardChannelAllocation CardChannelAllocation { get; set; }

        /// <summary>
        /// The program that is being recorded.
        /// </summary>
        public UpcomingProgram Program { get; set; }

        /// <summary>
        /// The time the recording actually started.
        /// </summary>
        public DateTime RecordingStartTime { get; set; }

        /// <summary>
        /// The full path of the recording file.
        /// </summary>
        public string RecordingFileName { get; set; }

        /// <summary>
        /// The list of programs that this program conflicts with. If CardChannelAllocation is
        /// null these are the programs that block the recording of this program, otherwise
        /// these are the programs that are blocked by this recording.
        /// </summary>
        public List<Guid> ConflictingPrograms { get; set; }

        /// <summary>
        /// The actual start time of the recording.  This overrules ActualStartTime in the
        /// Program and may contain a different time.
        /// </summary>
        public DateTime ActualStartTime { get; set; }

        /// <summary>
        /// The actual stop time of the recording.  This overrules ActualStopTime in the
        /// Program and may contain a different time.
        /// </summary>
        public DateTime ActualStopTime { get; set; }

        /// <summary>
        /// The actual start time of the recording (UTC).  This overrules ActualStartTime in the
        /// Program and may contain a different time.
        /// </summary>
        public DateTime ActualStartTimeUtc { get; set; }

        /// <summary>
        /// The actual stop time of the recording (UTC).  This overrules ActualStopTime in the
        /// Program and may contain a different time.
        /// </summary>
        public DateTime ActualStopTimeUtc { get; set; }

        /// <summary>
        /// The ID of the recording.
        /// </summary>
        public Guid RecordingId { get; set; }

        /// <summary>
        /// The integer ID of the recording.
        /// </summary>
        public int RecordingIntId { get; set; }
    }
}
