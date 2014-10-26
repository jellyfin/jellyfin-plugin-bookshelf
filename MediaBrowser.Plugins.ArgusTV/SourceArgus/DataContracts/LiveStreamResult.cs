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
    /// The result of a live stream method on a recorder.
    /// </summary>
    public enum LiveStreamResult
    {
        /// <summary>
        /// Everything went OK.
        /// </summary>
        Succeeded = 0,
        /// <summary>
        /// No free card was found to tune the channel.
        /// </summary>
        NoFreeCardFound = 1,
        /// <summary>
        /// Failed to tune to the requested channel.
        /// </summary>
        ChannelTuneFailed = 2,
        /// <summary>
        /// It was not possible to re-tune the existing stream, so you need to stop it and try with a new one.
        /// </summary>
        NoRetunePossible = 3,
        /// <summary>
        /// The requested channel was scrambled.
        /// </summary>
        IsScrambled = 4,
        /// <summary>
        /// An unknown error occurred.
        /// </summary>
        UnknownError = 98,
        /// <summary>
        /// Live streaming is not supported by this recorder.
        /// </summary>
        NotSupported = 99
    }
}
