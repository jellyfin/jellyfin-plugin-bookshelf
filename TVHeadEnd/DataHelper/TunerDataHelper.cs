using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TVHeadEnd.HTSP;

namespace TVHeadEnd.DataHelper
{
    public class TunerDataHelper
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, HTSMessage> _data;

        public TunerDataHelper(ILogger logger)
        {
            _logger = logger;
            _data = new Dictionary<string, HTSMessage>();
        }

        public void clean()
        {
            lock (_data)
            {
                _data.Clear();
            }
        }

        public void addTunerInfo(HTSMessage tunerMessage)
        {
            lock (_data)
            {
                string channelID = "" + tunerMessage.getInt("channelId");
                if (_data.ContainsKey(channelID))
                {
                    _data.Remove(channelID);
                }
                _data.Add(channelID, tunerMessage);
            }
        }


        /*
          <dump>
            channelId : 240
            channelNumber : 40
            channelName : zdf.kultur
            eventId : 11708150
            nextEventId : 11708152
            services :       name : CXD2837 DVB-C DVB-T/T2 (adapter 7)/KBW: 370,000 kHz/zdf.kultur
              type : SDTV
        ,       name : CXD2837 DVB-C DVB-T/T2  (adapter 6)/KBW: 370,000 kHz/zdf.kultur
              type : SDTV
        ,       name : STV0367 DVB-C DVB-T (adapter 5)/KBW: 370,000 kHz/zdf.kultur
              type : SDTV
        ,       name : STV0367 DVB-C DVB-T (adapter 4)/KBW: 370,000 kHz/zdf.kultur
              type : SDTV
        ,       name : CXD2837 DVB-C DVB-T/T2 (adapter 3)/KBW: 370,000 kHz/zdf.kultur
              type : SDTV
        ,       name : CXD2837 DVB-C DVB-T/T2 (adapter 2)/KBW: 370,000 kHz/zdf.kultur
              type : SDTV
        ,       name : STV0367 DVB-C DVB-T (adapter 1)/KBW: 370,000 kHz/zdf.kultur
              type : SDTV
        ,       name : STV0367 DVB-C DVB-T (adapter 0)/KBW: 370,000 kHz/zdf.kultur
              type : SDTV
        , 
            tags : 1, 2, 
            method : channelAdd
          </dump>
        */

        public Task<List<LiveTvTunerInfo>> buildTunerInfos(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew<List<LiveTvTunerInfo>>(() =>
            {
                List<LiveTvTunerInfo> result = new List<LiveTvTunerInfo>();
                lock (_data)
                {
                    foreach (HTSMessage currMessage in _data.Values)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.Info("[TVHclient] TunerDataHelper.buildTunerInfos: cancel requst received. Returning only partly results");
                            return result;
                        }

                        string channelId = "";
                        if (currMessage.containsField("channelId"))
                        {
                            channelId = "" + currMessage.getInt("channelId");
                        }

                        string programName = "";
                        if (currMessage.containsField("channelName"))
                        {
                            programName = currMessage.getString("channelName");
                        }

                        IList services = null;
                        if (currMessage.containsField("services"))
                        {
                            services = currMessage.getList("services");
                        }
                        if (services != null)
                        {
                            foreach (HTSMessage currService in services)
                            {
                                string name = "";
                                if (currService.containsField("name"))
                                {
                                    name = currService.getString("name");
                                }
                                else
                                {
                                    continue;
                                }
                                string type = "";
                                if (currService.containsField("type"))
                                {
                                    type = currService.getString("type");
                                }

                                LiveTvTunerInfo ltti = new LiveTvTunerInfo();
                                ltti.Id = name;
                                ltti.Name = name;
                                ltti.ProgramName = programName;
                                ltti.SourceType = type;
                                ltti.ChannelId = channelId;
                                ltti.Status = LiveTvTunerStatus.Available;

                                //ltti.Clients // not available from TVheadend
                                //ltti.RecordingId // not available from TVheadend

                                result.Add(ltti);
                            }
                        }
                    }
                }
                return result;
            });
        }
    }
}
