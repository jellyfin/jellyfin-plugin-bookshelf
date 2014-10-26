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
    /// Specified a time of a day.
    /// </summary>
    [XmlType("time")]
    public class ScheduleTime
    {
        private DateTime _dateTime;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ScheduleTime()
        {
        }

        /// <summary>
        /// Constructs a time based on hours, minutes and seconds.
        /// </summary>
        /// <param name="hours">The hour, from 0 to 23.</param>
        /// <param name="minutes">The minutes, from 0 to 59.</param>
        /// <param name="seconds">The seconds, from 0 to 59.</param>
        public ScheduleTime(int hours, int minutes, int seconds)
        {
            _dateTime = new DateTime(1900, 1, 1, hours, minutes, seconds, DateTimeKind.Local);
        }

        /// <summary>
        /// Constructs a time based on the hours, minutes and seconds in a timespan.
        /// </summary>
        /// <param name="timeSpan">The timespan containing the time of day.</param>
        public ScheduleTime(TimeSpan timeSpan)
        {
            this.Ticks = timeSpan.Ticks;
        }

        /// <summary>
        /// The hour of the time.
        /// </summary>
        [XmlIgnore]
        public int Hours
        {
            get { return _dateTime.TimeOfDay.Hours; }
        }

        /// <summary>
        /// The minutes of the time.
        /// </summary>
        [XmlIgnore]
        public int Minutes
        {
            get { return _dateTime.TimeOfDay.Minutes; }
        }

        /// <summary>
        /// The seconds of the time.
        /// </summary>
        [XmlIgnore]
        public int Seconds
        {
            get { return _dateTime.TimeOfDay.Seconds; }
        }

        /// <summary>
        /// The time of day in ticks.
        /// </summary>
        [XmlText]
        public long Ticks
        {
            get { return _dateTime.TimeOfDay.Ticks; }
            set { _dateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Local).AddTicks(value); }
        }

        /// <summary>
        /// Apply the time of day to a given date.
        /// </summary>
        /// <param name="dateTime">The date on which to get the time</param>
        /// <returns>A datetime on the given date and with the correct time of day.</returns>
        public DateTime ApplyToDate(DateTime dateTime)
        {
            return dateTime.Date.AddTicks(this.Ticks);
        }

        /// <summary>
        /// Get the time of day from a given datetime.
        /// </summary>
        /// <param name="dateTime">The datetime from which to get the time of day.</param>
        /// <returns>The time of day as a ScheduleTime.</returns>
        public static ScheduleTime FromDateTime(DateTime dateTime)
        {
            ScheduleTime result = new ScheduleTime();
            result.Ticks = dateTime.TimeOfDay.Ticks;
            return result;
        }
    }
}
