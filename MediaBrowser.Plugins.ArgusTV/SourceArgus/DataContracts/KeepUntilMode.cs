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
using System.Text;

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// The mode of the keep until setting for the recording.
    /// </summary>
    public enum KeepUntilMode
    {
        /// <summary>
        /// Keep the recording until space is needed.
        /// </summary>
        UntilSpaceIsNeeded = 0,
        /// <summary>
        /// Keep recording forever.
        /// </summary>
        Forever = 1,
        /// <summary>
        /// Keep recording for a number of days given by KeepUntilValue.
        /// </summary>
        NumberOfDays = 2,
        /// <summary>
        /// Keep recording if it's still part of the number of most recent episodes given by KeepUntilValue.
        /// </summary>
        NumberOfEpisodes = 3,
        /// <summary>
        /// Keep recording if it's still part of the number of most recent watched episodes given by KeepUntilValue. Unwatched recordings are unaffected.
        /// </summary>
        NumberOfWatchedEpisodes = 4,
    }
}
