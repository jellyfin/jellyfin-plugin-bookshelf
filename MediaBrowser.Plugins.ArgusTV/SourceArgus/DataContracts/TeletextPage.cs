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
    /// TeletextPage.
    /// </summary>
    public class TeletextPage
    {
        /// <summary>
        /// The page number of the current page.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// The sub-page number of the current page.
        /// </summary>
        public int SubPageNumber { get; set; }

        /// <summary>
        /// The content of the teletextpage.
        /// </summary>
        public byte[] Content { get; set; }

        /// <summary>
        /// The number of subpages of the teletextpage.
        /// </summary>
        public int SubPageCount { get; set; }

        /// <summary>
        /// The page number of the page linked to the red button, or null.
        /// </summary>
        public int? RedPageNumber { get; set; }

        /// <summary>
        /// The page number of the page linked to the green button, or null.
        /// </summary>
        public int? GreenPageNumber { get; set; }

        /// <summary>
        /// The page number of the page linked to the yellow button, or null.
        /// </summary>
        public int? YellowPageNumber { get; set; }

        /// <summary>
        /// The page number of the page linked to the blue button, or null.
        /// </summary>
        public int? BluePageNumber { get; set; }

        /// <summary>
        /// The page number of the next page, or null if there is no next page.
        /// </summary>
        public int? NextPageNumber { get; set; }

        /// <summary>
        /// The page number of the index page of the current section.
        /// </summary>
        public int? FastTextPageNumber { get; set; }
    }
}
