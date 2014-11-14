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
using System.Xml.Serialization;

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// The day(s) a schedule should be active. Any combination of these flags OR-ed together is possible.
    /// </summary>
    [Flags]
    [XmlType("days")]
    public enum ScheduleDaysOfWeek
    {
        /// <summary>
        /// No days.
        /// </summary>
        None = 0,
        /// <summary>
        /// All mondays.
        /// </summary>
        Mondays = (1 << DayOfWeek.Monday),
        /// <summary>
        /// All tuesdays.
        /// </summary>
        Tuesdays = (1 << DayOfWeek.Tuesday),
        /// <summary>
        /// All wednesdays.
        /// </summary>
        Wednesdays = (1 << DayOfWeek.Wednesday),
        /// <summary>
        /// All thursdays.
        /// </summary>
        Thursdays = (1 << DayOfWeek.Thursday),
        /// <summary>
        /// All fridays.
        /// </summary>
        Fridays = (1 << DayOfWeek.Friday),
        /// <summary>
        /// All saturdays.
        /// </summary>
        Saturdays = (1 << DayOfWeek.Saturday),
        /// <summary>
        /// All sundays.
        /// </summary>
        Sundays = (1 << DayOfWeek.Sunday),
        /// <summary>
        /// All working days (monday through friday).
        /// </summary>
        WorkingDays = ((1 << DayOfWeek.Monday) | (1 << DayOfWeek.Tuesday) | (1 << DayOfWeek.Wednesday) | (1 << DayOfWeek.Thursday) | (1 << DayOfWeek.Friday)),
        /// <summary>
        /// All weekends (saturdays and sundays).
        /// </summary>
        Weekends = ((1 << DayOfWeek.Saturday) | (1 << DayOfWeek.Sunday))
    }
}
