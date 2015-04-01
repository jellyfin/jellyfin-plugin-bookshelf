using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmbyTV.GeneralHelpers;

namespace EmbyTV.EPGProvider
{
    interface IEpgSupplier
    {
       // Task getStatus(HttpClientHelper httpClientHelper);
        //Task getTvGuide(HttpClientHelper httpClientHelper);
    }

    public class Headend
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }
}
