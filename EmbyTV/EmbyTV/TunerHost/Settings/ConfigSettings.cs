using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbyTV.TunerHost.Settings
{
    public class TunerHostSettings
    {
        public List<Constructor> Settings { get; set; }
       
    }
    public class Constructor
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string DefaultValue { get; set; }
        public string Label { get; set; }

    }
}
