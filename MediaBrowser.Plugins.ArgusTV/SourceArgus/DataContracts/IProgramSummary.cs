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
using System.Linq;
using System.Text;

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// Information about a program belonging to a guide channel.
    /// </summary>
    public interface IProgramSummary
    {
        /// <summary>
        /// The program's title.
        /// </summary>
        string Title { set; get; }

        /// <summary>
        /// The program's start time.
        /// </summary>
        DateTime StartTime { set; get; }

        /// <summary>
        /// The program's stop time.
        /// </summary>
        DateTime StopTime { set; get; }

        /// <summary>
        /// The program's episode title.
        /// </summary>
        string SubTitle { set; get; }

        /// <summary>
        /// The program's category.
        /// </summary>
        string Category { set; get; }

        /// <summary>
        /// Is this program a repeat?
        /// </summary>
        bool IsRepeat { set; get; }

        /// <summary>
        /// Is this program a premiere?
        /// </summary>
        bool IsPremiere { set; get; }

        /// <summary>
        /// The program's flags defining things like aspect ratio, SD or HD,...
        /// </summary>
        GuideProgramFlags Flags { set; get; }

        /// <summary>
        /// A string to display the episode number in a UI.
        /// </summary>
        string EpisodeNumberDisplay { set; get; }

        /// <summary>
        /// The parental rating of the program.
        /// </summary>
        string Rating { set; get; }

        /// <summary>
        /// If set, a star-rating of the program, normalized to a value between 0 and 1.
        /// </summary>
        double? StarRating { set; get; }

        #region Methods

        /// <summary>
        /// Create a single string containing the full program title (with episode information).
        /// </summary>
        /// <returns>A string with the full program title.</returns>
        string CreateProgramTitle();

        /// <summary>
        /// Create a single string with episode information (episode title and/or number).
        /// </summary>
        /// <returns>A string with all episode information.</returns>
        string CreateEpisodeTitle();

        #endregion
    }
}
