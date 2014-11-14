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
    /// An upcoming recording.
    /// </summary>
    public partial class UpcomingRecording : IProgramSummary
	{
        /// <summary>
        /// Default constructor.
        /// </summary>
        public UpcomingRecording()
	    {
            this.ConflictingPrograms = new List<Guid>();
	    }

        /// <summary>
        /// The card that was allocated for the required channel. If this is null the program
        /// has not been allocated and won't be recorded.
        /// </summary>
        public CardChannelAllocation CardChannelAllocation { get; set;}

        /// <summary>
        /// The program that will be recorded.
        /// </summary>
        public UpcomingProgram Program { get; set; }

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

        #region IProgramSummary Members

        /// <summary>
        /// The program's title.
        /// </summary>
        public string Title
        {
            get { return this.Program.Title; }
            set { this.Program.Title = value; }
        }

        /// <summary>
        /// The program's start time.
        /// </summary>
        public DateTime StartTime
        {
            get { return this.Program.StartTime; }
            set { this.Program.StartTime = value; }
        }

        /// <summary>
        /// The program's stop time.
        /// </summary>
        public DateTime StopTime
        {
            get { return this.Program.StopTime; }
            set { this.Program.StopTime = value; }
        }

        /// <summary>
        /// The program's start time (UTC).
        /// </summary>
        public DateTime StartTimeUtc
        {
            get { return this.Program.StartTimeUtc; }
            set { this.Program.StartTimeUtc = value; }
        }

        /// <summary>
        /// The program's stop time (UTC).
        /// </summary>
        public DateTime StopTimeUtc
        {
            get { return this.Program.StopTimeUtc; }
            set { this.Program.StopTimeUtc = value; }
        }

        /// <summary>
        /// The program's episode title.
        /// </summary>
        public string SubTitle
        {
            get { return this.Program.SubTitle; }
            set { this.Program.SubTitle = value; }
        }

        /// <summary>
        /// The program's category.
        /// </summary>
        public string Category
        {
            get { return this.Program.Category; }
            set { this.Program.Category = value; }
        }

        /// <summary>
        /// Is this program a repeat?
        /// </summary>
        public bool IsRepeat
        {
            get { return this.Program.IsRepeat; }
            set { this.Program.IsRepeat = value; }
        }

        /// <summary>
        /// Is this program a premiere?
        /// </summary>
        public bool IsPremiere
        {
            get { return this.Program.IsPremiere; }
            set { this.Program.IsPremiere = value; }
        }

        /// <summary>
        /// The program's flags defining things like aspect ratio, SD or HD,...
        /// </summary>
        public GuideProgramFlags Flags
        {
            get { return this.Program.Flags; }
            set { this.Program.Flags = value; }
        }

        /// <summary>
        /// A string to display the episode number in a UI.
        /// </summary>
        public string EpisodeNumberDisplay
        {
            get { return this.Program.EpisodeNumberDisplay; }
            set { this.Program.EpisodeNumberDisplay = value; }
        }

        /// <summary>
        /// The parental rating of the program.
        /// </summary>
        public string Rating
        {
            get { return this.Program.Rating; }
            set { this.Program.Rating = value; }
        }

        /// <summary>
        /// If set, a star-rating of the program, normalized to a value between 0 and 1.
        /// </summary>
        public double? StarRating
        {
            get { return this.Program.StarRating; }
            set { this.Program.StarRating = value; }
        }

        /// <summary>
        /// Create a single string containing the full program title (with episode information).
        /// </summary>
        /// <returns>A string with the full program title.</returns>
        public string CreateProgramTitle()
        {
            return this.Program.CreateProgramTitle();
        }

        /// <summary>
        /// Create a single string with episode information (episode title and/or number).
        /// </summary>
        /// <returns>A string with all episode information.</returns>
        public string CreateEpisodeTitle()
        {
            return this.Program.CreateEpisodeTitle();
        }

        #endregion
    }
}
