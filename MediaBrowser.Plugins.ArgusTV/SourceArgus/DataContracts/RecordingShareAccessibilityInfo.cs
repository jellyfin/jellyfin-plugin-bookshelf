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
    /// Informaton about a RecordingShare.
    /// </summary>
    public partial class RecordingShareAccessibilityInfo
    {
        /// <summary>
        /// The unique Id of the recorder.
        /// </summary>
        public Guid RecorderTunerId { get; set; }

        /// <summary>
        /// The friendly name of the recorder.
        /// </summary>
        public string RecorderTunerName { get; set; }

        /// <summary>
        /// The Share location.
        /// </summary>
        public string Share { get; set; }

        /// <summary>
        /// Indication if the share is accessible for read and write file operations.
        /// </summary>
        public bool ShareAccessible { get; set; }
    }
}
