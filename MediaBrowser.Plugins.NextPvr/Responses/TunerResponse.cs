using System.Collections.Generic;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    class TunerResponse
    {

        public class Tuner
        {
            public string tunerName { get; set; }
            public string tunerStatus { get; set; }
            public List<string> recordings { get; set; } 
            public List<string> liveTV { get; set; }
        }

        public class RootObject
        {
            public List<Tuner> Tuners { get; set; }
        }
    }
}
