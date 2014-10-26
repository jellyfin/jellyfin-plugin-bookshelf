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
    /// ARGUS TV event groups where listeners can subscribe to.
    /// </summary>
    [Flags]
    public enum EventGroup
    {
        /// <summary>
        /// Events related to ARGUS TV in general.
        /// </summary>
        SystemEvents = 0x00000001,
        /// <summary>
        /// Events related to guidedata.
        /// </summary>
        GuideEvents = 0x00000002,
        /// <summary>
        /// Events related to scheduling.
        /// </summary>
        ScheduleEvents = 0x00000004,
        /// <summary>
        /// Events related to recordings.
        /// </summary>
        RecordingEvents = 0x00000008,
    }
}
