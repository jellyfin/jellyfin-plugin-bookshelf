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
    /// A format string to build a recording file's path and filename.
    /// </summary>
    public partial class RecordingFileFormat
    {
        /// <summary>
        /// The default recording file format.
        /// </summary>
        public const string DefaultFormat = @"%%TITLE%%\%%LONGTITLE%%_%%CHANNEL%%_%%DATE%%_%%HOURS%%-%%MINUTES%%";

        /// <summary>
        /// The unique ID of the recording format.
        /// </summary>
        public Guid RecordingFileFormatId { get; set; }

        /// <summary>
        /// The name of the recording format.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The format string.
        /// </summary>
        public string Format { get; set; }
    }
}
