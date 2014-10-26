using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// A polling event from the ARGUS TV Scheduler service.
    /// </summary>
    public class ServiceEvent
    {
        /// <summary>
        /// The name of the event (corresponding to event listener WCF service operations).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The arguments of the event (if any).
        /// </summary>
        public object[] Arguments { get; set; }

        /// <summary>
        /// The time of the event.
        /// </summary>
        public DateTime Time { get; set; }
    }
}
