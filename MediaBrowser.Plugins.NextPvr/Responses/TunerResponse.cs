using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    public class TunerResponse
    {
        private readonly RootObject _root;

        public TunerResponse(Stream stream, IJsonSerializer json)
        {
           _root = json.DeserializeFromStream<RootObject>(stream);

        }
        public List<LiveTvTunerInfo> LiveTvTunerInfos()
        {
            return _root.Tuners.Select(i => new LiveTvTunerInfo()
            {
                Name = i.tunerName               
            }).ToList();
        }

        public class Recording2
        {
            public int tunerOID { get; set; }
            public string recName { get; set; }
            public int channelOID { get; set; }
            public int recordingOID { get; set; }
        }

        public class Recording
        {
            public Recording2 Recording2 { get; set; }
        }

        public class Tuner
        {
            public string tunerName { get; set; }
            public string tunerStatus { get; set; }
            public List<Recording> recordings { get; set; }
            public List<object> liveTV { get; set; }
        }

        public class RootObject
        {
            public List<Tuner> Tuners { get; set; }
        }
    }
}
