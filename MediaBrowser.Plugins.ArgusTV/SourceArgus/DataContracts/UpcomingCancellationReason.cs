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
    /// The reason an upcoming program was flagged as cancelled.
    /// </summary>
    public enum UpcomingCancellationReason
    {
        /// <summary>
        /// The program is not cancelled.
        /// </summary>
        None = 0,
        /// <summary>
        /// The user manually cancelled this program.
        /// </summary>
        Manual = 1,
        /// <summary>
        /// Only on upcoming recordings with the NewEpisodesOnly or NewTitlesOnly rule.
        /// The program was cancelled because an earlier broadcast is already queued to be recorded.
        /// </summary>
        AlreadyQueued = 2,
        /// <summary>
        /// Only on upcoming recordings with the NewEpisodesOnly or NewTitlesOnly rule.
        /// The program was cancelled because it was previously recorded.
        /// </summary>
        PreviouslyRecorded = 3
    }
}
