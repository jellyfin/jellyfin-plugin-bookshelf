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
    /// The priority of a schedule.
    /// </summary>
    public enum SchedulePriority
    {
        /// <summary>
        /// Very low priority schedule.
        /// </summary>
        VeryLow = -2,
        /// <summary>
        /// Low priority schedule.
        /// </summary>
        Low = -1,
        /// <summary>
        /// Normal priority schedule.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// High priority schedule.
        /// </summary>
        High = 1,
        /// <summary>
        /// Very high priority schedule.
        /// </summary>
        VeryHigh = 2
    }
}
