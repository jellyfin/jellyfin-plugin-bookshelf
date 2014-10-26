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
using System.Xml.Serialization;

namespace ArgusTV.DataContracts
{
	/// <summary>
    /// An entry in the history of recorded programs for a schedule.
	/// </summary>
    [XmlType("recordedProgram")]
    public partial class ScheduleRecordedProgram
	{
		/// <summary>
        /// The unique ID of the recorded program entry.
		/// </summary>
        [XmlIgnore]
        public int ScheduleRecordedProgramId { get; set; }

		/// <summary>
        /// The ID of the schedule this recording belongs to.
		/// </summary>
        [XmlIgnore]
        public Guid ScheduleId { get; set; }

        /// <summary>
        /// The title of the recording.
        /// </summary>
        [XmlAttribute("title")]
        public string Title { get; set; }

        /// <summary>
        /// The episode title of the recording.
		/// </summary>
        [XmlAttribute("episode")]
        public string Episode { get; set; }

		/// <summary>
        /// The start time of the recorded program.
		/// </summary>
        [XmlAttribute("recordedOn")]
        public DateTime RecordedOn { get; set; }
	}
}
