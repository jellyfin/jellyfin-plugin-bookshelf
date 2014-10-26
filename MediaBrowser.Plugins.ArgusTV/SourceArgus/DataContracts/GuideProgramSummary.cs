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
using System.Text;
using System.Collections;

namespace ArgusTV.DataContracts
{
	/// <summary>
	/// A light-weight version of the information about a program belonging to a guide channel.
	/// </summary>
    public partial class GuideProgramSummary : IProgramSummary
	{
		/// <summary>
        /// The unique ID of the guide program.
		/// </summary>
		public Guid GuideProgramId { get; set; }

        /// <summary>
        /// The unique integer ID of the guide program.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ID of the channel this program belongs to.
		/// </summary>
        public Guid GuideChannelId { get; set; }

        /// <summary>
        /// The program's title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The program's start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The program's stop time.
        /// </summary>
        public DateTime StopTime { get; set; }

        /// <summary>
        /// The program's start time (UTC).
        /// </summary>
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// The program's stop time (UTC).
        /// </summary>
        public DateTime StopTimeUtc { get; set; }

        /// <summary>
        /// The program's episode title.
        /// </summary>
        public string SubTitle { get; set; }

        /// <summary>
        /// The program's category.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Is this program a repeat?
        /// </summary>
        public bool IsRepeat { get; set; }

        /// <summary>
        /// Is this program a premiere?
        /// </summary>
        public bool IsPremiere { get; set; }

        /// <summary>
        /// The program's flags defining things like aspect ratio, SD or HD,...
        /// </summary>
        public GuideProgramFlags Flags { get; set; }

        /// <summary>
        /// If known, the series number this program belongs to.
        /// </summary>
        public int? SeriesNumber { get; set; }

        /// <summary>
        /// A string to display the episode number in a UI.
        /// </summary>
        public string EpisodeNumberDisplay { get; set; }

        /// <summary>
        /// If known, the episode number of the program.
        /// </summary>
        public int? EpisodeNumber { get; set; }

        /// <summary>
        /// If known, the total number of episodes in the current series.
        /// </summary>
        public int? EpisodeNumberTotal { get; set; }

        /// <summary>
        /// If known and if applicable, the episode part number of the program.
        /// </summary>
        public int? EpisodePart { get; set; }

        /// <summary>
        /// If known and if applicable, the total number of parts.
        /// </summary>
        public int? EpisodePartTotal { get; set; }

        /// <summary>
        /// The parental rating of the program.
        /// </summary>
        public string Rating { get; set; }

        /// <summary>
        /// If set, a star-rating of the program, normalized to a value between 0 and 1.
        /// </summary>
        public double? StarRating { get; set; }

        #region Miscellaneous

        /// <summary>
        /// Create a single string containing the full program title (with episode information).
        /// </summary>
        /// <returns>A string with the full program title.</returns>
        public string CreateProgramTitle()
        {
            return GuideProgram.CreateProgramTitle(this.Title, this.SubTitle, this.EpisodeNumberDisplay);
        }

        /// <summary>
        /// Create a single string with episode information (episode title and/or number).
        /// </summary>
        /// <returns>A string with all episode information.</returns>
        public string CreateEpisodeTitle()
        {
            return GuideProgram.CreateEpisodeTitle(this.SubTitle, this.EpisodeNumberDisplay);
        }

        /// <summary>
        /// Create a unique upcoming program ID for a guide program that is scheduled on
        /// a specific channel.
        /// </summary>
        /// <param name="channelId">The ID of the channel.</param>
        /// <returns>The unique upcoming program ID.</returns>
        public Guid GetUniqueUpcomingProgramId(Guid channelId)
        {
            return UpcomingProgram.GetUniqueUpcomingProgramId(this.GuideProgramId, channelId);
        }

        #endregion
    }
}
