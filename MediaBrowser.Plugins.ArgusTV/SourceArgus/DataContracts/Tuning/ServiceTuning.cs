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
using System.Linq;
using System.Text;

namespace ArgusTV.DataContracts.Tuning
{
    /// <summary>
    /// A service's tuning details.
    /// </summary>
    public class ServiceTuning
    {
        /// <summary>
        /// The type of card of this tuning.
        /// </summary>
        public CardType CardType { get; set; }

        /// <summary>
        /// ATSC, DVB-C, DVB-S, DVB-T, DVB-IP, Analog
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// DVB-C, DVB-S, DVB-T, DVB-IP
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// The signal quality of the service (if available).
        /// </summary>
        public int SignalQuality { get; set; }

        /// <summary>
        /// The signal strength of the service (if available).
        /// </summary>
        public int SignalStrength { get; set; }

        /// <summary>
        /// ATSC, DVB-C, DVB-S, DVB-T, DVB-IP, Analog
        /// </summary>
        public bool IsFreeToAir { get; set; }

        /// <summary>
        /// ATSC, DVB-C, DVB-S, DVB-T, Analog
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// DVB-C, DVB-S, DVB-T
        /// </summary>
        public int ONID { get; set; }

        /// <summary>
        /// ATSC, DVB-C, DVB-S, DVB-T
        /// </summary>
        public int TSID { get; set; }

        /// <summary>
        /// ATSC, DVB-C, DVB-S, DVB-T
        /// </summary>
        public int SID { get; set; }

        /// <summary>
        /// ATSC, DVB-C, DVB-S, DVB-T
        /// </summary>
        public FecCodeRate InnerFecRate { get; set; }

        /// <summary>
        /// ATSC, DVB-C, DVB-S, DVB-T
        /// </summary>
        public Modulation Modulation { get; set; }

        /// <summary>
        /// ATSC, DVB-C, DVB-S
        /// </summary>
        public int SymbolRate { get; set; }

        /// <summary>
        /// DVB-C
        /// </summary>
        public FecCodeRate OuterFecRate { get; set; }

        /// <summary>
        /// ATSC, Analog
        /// </summary>
        public int PhysicalChannel { get; set; }

        /// <summary>
        /// DVB-IP
        /// </summary>
        public string Url { get; set; }

        #region ATSC only

        /// <summary>
        /// ATSC
        /// </summary>
        public int MajorChannel { get; set; }

        /// <summary>
        /// ATSC
        /// </summary>
        public int MinorChannel { get; set; }

        #endregion

        #region DVB-S only

        /// <summary>
        /// DVB-S
        /// </summary>
        public int OrbitalPosition { get; set; }

        /// <summary>
        /// DVB-S
        /// </summary>
        public SignalPolarisation SignalPolarisation { get; set; }

        /// <summary>
        /// DVB-S
        /// </summary>
        public bool IsDvbS2 { get; set; }

        /// <summary>
        /// DVB-S
        /// </summary>
        public RollOff RollOff { get; set; }

        /// <summary>
        /// DVB-S
        /// </summary>
        public Pilot Pilot { get; set; }

        #endregion

        #region DVB-T only

        /// <summary>
        /// DVB-T
        /// </summary>
        public int Bandwidth { get; set; }

        /// <summary>
        /// DVB-T
        /// </summary>
        public FecCodeRate LPInnerFecRate { get; set; }

        /// <summary>
        /// DVB-T
        /// </summary>
        public GuardInterval GuardInterval { get; set; }

        /// <summary>
        /// DVB-T
        /// </summary>
        public TransmissionMode TransmissionMode { get; set; }

        /// <summary>
        /// DVB-T
        /// </summary>
        public HAlpha HierarchyAlpha { get; set; }

        #endregion
    }
}
