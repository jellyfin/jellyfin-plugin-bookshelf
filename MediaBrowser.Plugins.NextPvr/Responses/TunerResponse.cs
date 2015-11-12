using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Drawing;
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
            return _root.Tuners.Select(GetTunerInformation).ToList();
        }

        private LiveTvTunerInfo GetTunerInformation(Tuner i)
        {
            LiveTvTunerInfo tunerinfo = new LiveTvTunerInfo();

            tunerinfo.Name = i.tunerName;
            tunerinfo.Status = GetStatus(i);

            if (i.recordings.Count > 0)
            {
                tunerinfo.ChannelId = i.recordings.Single().Recording.channelOID.ToString();
            }

            return tunerinfo;
        }

        private LiveTvTunerStatus GetStatus(Tuner i)
        {
            if (i.recordings.Count > 0)
            {
                return LiveTvTunerStatus.RecordingTv;
            }

            if (i.liveTV.Count > 0)
            {
                return LiveTvTunerStatus.LiveTv;
            }

            return LiveTvTunerStatus.Available;
        }

        public class Recording
        {
            public int tunerOID { get; set; }
            public string recName { get; set; }
            public int channelOID { get; set; }
            public int recordingOID { get; set; }
        }

        public class Recordings
        {
            public Recording Recording { get; set; }
        }

        public class Tuner
        {
            public string tunerName { get; set; }
            public string tunerStatus { get; set; }
            public List<Recordings> recordings { get; set; }
            public List<object> liveTV { get; set; }
        }

        public class RootObject
        {
            public List<Tuner> Tuners { get; set; }
        }
    }
}
