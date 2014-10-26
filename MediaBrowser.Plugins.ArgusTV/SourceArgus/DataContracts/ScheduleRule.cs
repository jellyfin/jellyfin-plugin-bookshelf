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
    /// A schedule rule.
    /// </summary>
    [XmlType("rule")]
    public class ScheduleRule
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ScheduleRule()
        {
            this.Arguments = new List<object>();
        }

        /// <summary>
        /// Construct a schedule rule with a given type and optional arguments.
        /// </summary>
        /// <param name="type">The type of the rule.</param>
        /// <param name="args">Optional arguments to the rule.</param>
        public ScheduleRule(ScheduleRuleType type, params object[] args)
        {
            this.Type = type;
            this.Arguments = new List<object>();
            foreach (object arg in args)
            {
                this.Arguments.Add(arg);
            }
        }

        /// <summary>
        /// The type of the rule.
        /// </summary>
        [XmlAttribute("type")]
        public ScheduleRuleType Type { get; set; }

        /// <summary>
        /// One or more arguments to the rule (if it has any).
        /// </summary>
        [XmlArray("args")]
        public List<object> Arguments { get; set; }
    }
}
