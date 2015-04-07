using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.TVHclient.Helper
{
    public class DateTimeHelper
    {
        public static long getUnixUTCTimeFromUtcDateTime(DateTime utcTime)
        {
            //create Timespan by subtracting the value provided from the Unix Epoch
            TimeSpan span = (utcTime - new DateTime(1970, 1, 1, 0, 0, 0, 0));

            //return the total seconds (which is a UNIX timestamp)
            return (long)span.TotalSeconds;
        }
    }
}
