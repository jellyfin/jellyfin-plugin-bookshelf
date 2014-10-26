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
    /// The priority of an upcoming recording.
    /// </summary>
    public enum UpcomingProgramPriority
    {
        /// <summary>
        /// Very low priority program.
        /// </summary>
        VeryLow = -2,
        /// <summary>
        /// Low priority program.
        /// </summary>
        Low = -1,
        /// <summary>
        /// Normal priority program.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// High priority program.
        /// </summary>
        High = 1,
        /// <summary>
        /// Very high priority program.
        /// </summary>
        VeryHigh = 2,
        /// <summary>
        /// Highest priority program.
        /// </summary>
        Highest = 3
    }
}
