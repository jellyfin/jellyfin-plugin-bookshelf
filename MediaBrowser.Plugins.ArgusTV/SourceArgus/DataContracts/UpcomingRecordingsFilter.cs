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
    [Flags]
    public enum UpcomingRecordingsFilter
    {
        /// <summary>
        /// Return all upcoming recordings, both normal and cancelled.
        /// </summary>
        All = Recordings | CancelledByUser | CancelledByRule,
        /// <summary>
        /// Return upcoming recordings.
        /// </summary>
        Recordings = 0x1,
        /// <summary>
        /// Return upcoming recordings that where explicitly cancelled by the user.
        /// </summary>
        CancelledByUser = 0x2,
        /// <summary>
        /// Return upcoming recordings that where cancelled by ARGUS TV due to a NewEpisodesOnly or NewTitlesOnly rule.
        /// </summary>
        CancelledByRule = 0x4,
    }
}
