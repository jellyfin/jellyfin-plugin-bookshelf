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
	/// Information about a program belonging to a guide channel.
	/// </summary>
    [Serializable]
    public partial class GuideProgram : IProgramSummary
	{
        /// <summary>
        /// The default constructor.
        /// </summary>
        public GuideProgram()
        {
            this.Directors = new string[0];
            this.Actors = new string[0];
        }

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
        /// The program's start time (UTC).
        /// </summary>
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// The program's stop time.
        /// </summary>
        public DateTime StopTime { get; set; }

        /// <summary>
        /// The program's stop time (UTC).
        /// </summary>
        public DateTime StopTimeUtc { get; set; }

        /// <summary>
        /// When the program was previously aired.
        /// </summary>
        public DateTime? PreviouslyAiredTime { get; set; }

        /// <summary>
        /// The program's episode title.
        /// </summary>
        public string SubTitle { get; set; }

        /// <summary>
        /// The program's description.
        /// </summary>
        public string Description { get; set; }

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

        /// <summary>
        /// The director(s) of the program.
        /// </summary>
        public string[] Directors { get; set; }

        /// <summary>
        /// The actors appearing in the program.
        /// </summary>
        public string[] Actors { get; set; }

        /// <summary>
        /// The ID of the series or null (e.g. for OpenTV).
        /// </summary>
        public string SeriesId { get; set; }

        /// <summary>
        /// The termination flag for the series (e.g. for OpenTV).
        /// </summary>
        public bool IsSeriesTermination { get; set; }

        /// <summary>
        /// The time the record was last modified.
        /// </summary>
        public DateTime LastModifiedTime { get; set; }

        /// <summary>
        /// If set to true, the program has been deleted from the guide by the importer.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// INTERNAL USE ONLY.
        /// </summary>
        public int Version { get; set; }

        #region Miscellaneous

        /// <summary>
        /// Create a single string containing the full program title (with episode information).
        /// </summary>
        /// <returns>A string with the full program title.</returns>
        public string CreateProgramTitle()
        {
            return CreateProgramTitle(this.Title, this.SubTitle, this.EpisodeNumberDisplay);
        }

        /// <summary>
        /// Create a single string with episode information (episode title and/or number).
        /// </summary>
        /// <returns>A string with all episode information.</returns>
        public string CreateEpisodeTitle()
        {
            return CreateEpisodeTitle(this.SubTitle, this.EpisodeNumberDisplay);
        }

        /// <summary>
        /// Create a single string containing the full program title (with episode information).
        /// </summary>
        /// <param name="title">The title of the program.</param>
        /// <param name="subTitle">The episode title, or null.</param>
        /// <param name="episodeNumberDisplay">The episode number, or null.</param>
        /// <returns>A string with the full program title.</returns>
        public static string CreateProgramTitle(string title, string subTitle, string episodeNumberDisplay)
        {
            StringBuilder programTitle = new StringBuilder(title);
            if (!String.IsNullOrEmpty(subTitle) || !String.IsNullOrEmpty(episodeNumberDisplay))
            {
                programTitle.Append(" (");
                if (!String.IsNullOrEmpty(subTitle))
                {
                    programTitle.Append(subTitle);
                }
                if (!String.IsNullOrEmpty(episodeNumberDisplay)
                    && episodeNumberDisplay != subTitle)
                {
                    if (!String.IsNullOrEmpty(subTitle))
                    {
                        programTitle.Append(" - ");
                    }
                    programTitle.Append(episodeNumberDisplay);
                }
                programTitle.Append(")");
            }
            return programTitle.ToString();
        }

        /// <summary>
        /// Create a single string with episode information (episode title and/or number).
        /// </summary>
        /// <param name="subTitle">The episode title, or null.</param>
        /// <param name="episodeNumberDisplay">The episode number, or null.</param>
        /// <returns>All episode information combined in a single string.</returns>
        public static string CreateEpisodeTitle(string subTitle, string episodeNumberDisplay)
        {
            string episodeTitle = String.Empty;
            if (!String.IsNullOrEmpty(subTitle)
                || !String.IsNullOrEmpty(episodeNumberDisplay))
            {
                if (String.IsNullOrEmpty(subTitle)
                    || subTitle == episodeNumberDisplay)
                {
                    episodeTitle = episodeNumberDisplay;
                }
                else if (String.IsNullOrEmpty(episodeNumberDisplay))
                {
                    episodeTitle = subTitle;
                }
                else
                {
                    episodeTitle = subTitle + " (" + episodeNumberDisplay + ")";
                }
            }
            return episodeTitle;
        }

        /// <summary>
        /// Create a combined description containing the episode title (optional), the actual description and the program's director and actors.
        /// </summary>
        /// <param name="includeEpisodeTitle">Set to true to include the episode title.</param>
        /// <returns>The combined description.</returns>
        public string CreateCombinedDescription(bool includeEpisodeTitle)
        {
            return CreateCombinedDescription(includeEpisodeTitle ? CreateEpisodeTitle() : null, this.Description, this.Directors, this.Actors);
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

        internal static string CreateCombinedDescription(string description, string[] directors, string[] actors)
        {
            return CreateCombinedDescription(null, description, directors, actors);
        }

        internal static string CreateCombinedDescription(string episodeTitle, string description, string[] directors, string[] actors)
        {
            StringBuilder result = new StringBuilder();
            if (!String.IsNullOrEmpty(episodeTitle))
            {
                result.Append('"').Append(episodeTitle).Append('"');
            }
            if (!String.IsNullOrEmpty(description))
            {
                if (result.Length > 0)
                {
                    result.Append(" - ");
                }
                result.Append(description);
            }
            bool bracketAdded = false;
            if (directors.Length > 0)
            {
                result.Append(" (");
                bracketAdded = true;
                AppendCredits(result, directors);
            }
            if (actors.Length > 0)
            {
                if (bracketAdded)
                {
                    result.Append(" - ");
                }
                else
                {
                    result.Append(" (");
                    bracketAdded = true;
                }
                AppendCredits(result, actors);
            }
            if (bracketAdded)
            {
                result.Append(")");
            }
            return result.ToString();
        }

        private static void AppendCredits(StringBuilder result, string[] credits)
        {
            bool isFirst = true;
            foreach (string credit in credits)
            {
                string person = credit.Trim();
                if (!String.IsNullOrEmpty(person))
                {
                    if (!isFirst)
                    {
                        result.Append(", ");
                    }
                    result.Append(person);
                    isFirst = false;
                }
            }
        }

        #endregion
    }
}
