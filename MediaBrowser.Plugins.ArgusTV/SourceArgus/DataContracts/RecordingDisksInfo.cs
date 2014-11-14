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
    /// Information about all recording disks.
    /// </summary>
    public partial class RecordingDisksInfo
    {
        /// <summary>
        /// The total size of the disk.
        /// </summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// The amount of free space in bytes.
        /// </summary>
        public long FreeSpaceBytes { get; set; }

        /// <summary>
        /// The estimated amount of free space in hours for SD recordings.
        /// </summary>
        public double FreeHoursSD { get; set; }

        /// <summary>
        /// The estimated amount of free space in hours for HD recordings.
        /// </summary>
        public double FreeHoursHD { get; set; }

        /// <summary>
        /// The disk space used as a percentage.
        /// </summary>
        public double PercentageUsed { get; set; }

        /// <summary>
        /// Get more details about each recording disk.
        /// </summary>
        public RecordingDiskInfo[] RecordingDiskInfos { get; set; }
    }
}
