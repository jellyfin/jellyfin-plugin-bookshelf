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
using System.Globalization;
using System.Runtime.Serialization;

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// Thrown when an unexpected error has occurred in ARGUS TV.
    /// </summary>
    [Serializable]
    public class ArgusTVUnexpectedErrorException : ArgusTVException, ISerializable
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ArgusTVUnexpectedErrorException()
        {
        }

        /// <summary>
        /// Constructs an unexpected exception instance with an error message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ArgusTVUnexpectedErrorException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs an unexpected exception instance with an error message.
        /// </summary>
        /// <param name="message">The error message format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public ArgusTVUnexpectedErrorException(string message, params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, message, args))
        {
        }

        /// <summary>
        /// Constructs an unexpected exception instance with an error message.
        /// </summary>
        /// <param name="innerException">The original exception.</param>
        /// <param name="message">The error message format string.</param>
        public ArgusTVUnexpectedErrorException(Exception innerException, string message)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructs an unexpected exception instance with an error message.
        /// </summary>
        /// <param name="innerException">The original exception.</param>
        /// <param name="message">The error message format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public ArgusTVUnexpectedErrorException(Exception innerException, string message, params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, message, args), innerException)
        {
        }

        /// <summary>
        /// Initializes an unexpected exception instance with serialized data.
        /// </summary>
        /// <param name="info">The System.Runtime.Serialization.SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext that contains contextual information about the source or destination.</param>
        public ArgusTVUnexpectedErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
