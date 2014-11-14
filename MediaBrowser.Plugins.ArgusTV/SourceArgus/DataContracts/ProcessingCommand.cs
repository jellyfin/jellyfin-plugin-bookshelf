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
using System.Collections;

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// A post/live/delete processing command for recordings.
    /// </summary>
    public class ProcessingCommand
    {
        /// <summary>
        /// The unique ID of the processing command.
        /// </summary>
        public Guid ProcessingCommandId { get; set; }

        /// <summary>
        /// The name of the processing command.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The full path to the command.
        /// </summary>
        public string CommandPath { get; set; }

        /// <summary>
        /// The command arguments.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Is this command assigned to new television schedules by default?
        /// </summary>
        public bool IsDefaultTelevision { get; set; }

        /// <summary>
        /// Is this command assigned to new radio schedules by default?
        /// </summary>
        public bool IsDefaultRadio { get; set; }

        /// <summary>
        /// Defines when the command runs.
        /// </summary>
        public ProcessingRunMode RunMode { get; set; }

        /// <summary>
        /// If the run mode is PostAtTime this is the hours part of the time to run the command.
        /// </summary>
        public int? RunAtHours { get; set; }

        /// <summary>
        /// If the run mode is PostAtTime this is the minutes part of the time to run the command.
        /// </summary>
        public int? RunAtMinutes { get; set; }
    }
}
