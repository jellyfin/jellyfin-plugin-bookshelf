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
    public class ChannelDataHelper
    {
        private readonly ILogger _logger;
        private readonly TunerDataHelper _tunerDataHelper;
        private readonly Dictionary<int, HTSMessage> _data;

        public ChannelDataHelper(ILogger logger, TunerDataHelper tunerDataHelper)
        {
            _logger = logger;
            _tunerDataHelper = tunerDataHelper;
            _data = new Dictionary<int, HTSMessage>();
        }

        public void clean()
        {
            lock (_data)
            {
                _data.Clear();
                _tunerDataHelper.clean();
            }
        }

        public void add(HTSMessage message)
        {
            _tunerDataHelper.addTunerInfo(message);

            lock (_data)
            {
                if (_data.ContainsKey(message.getInt("channelId")))
                {
                    int channelID = message.getInt("channelId");
                    HTSMessage storedMessage = _data[channelID];
                    if (storedMessage != null)
                    {
                        foreach (KeyValuePair<string, object> entry in message)
                        {
                            if (storedMessage.containsField(entry.Key))
                            {
                                storedMessage.removeField(entry.Key);
                            }
                            storedMessage.putField(entry.Key, entry.Value);
                        }
                    }
                    else
                    {
                        _logger.Error("[TVHclient] ChannelDataHelper: update for channelID '" + channelID + "' but no initial data found!");
                    }
                }
                else
                {
                    if (message.containsField("channelNumber") && message.getInt("channelNumber") > 0) // use only channels with number > 0
                    {
                        _data.Add(message.getInt("channelId"), message);
                    }
                }
            }
        }

        public Task<IEnumerable<ChannelInfo>> buildChannelInfos(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew<IEnumerable<ChannelInfo>>(() =>
            {
                lock (_data)
                {
                    List<ChannelInfo> result = new List<ChannelInfo>();
                    foreach (KeyValuePair<int, HTSMessage> entry in _data)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.Info("[TVHclient] ChannelDataHelper.buildChannelInfos, call canceled - returning part list.");
                            return result;
                        }

                        HTSMessage m = entry.Value;

                        ChannelInfo ci = new ChannelInfo();
                        ci.Id = "" + entry.Key;

                        ci.ImagePath = "";

                        if (m.containsField("channelIcon"))
                            ci.ImageUrl = m.getString("channelIcon");

                        if (m.containsField("channelName"))
                        {
                            string name = m.getString("channelName");
                            if (string.IsNullOrEmpty(name))
                            {
                                continue;
                            }
                            ci.Name = m.getString("channelName");
                        }

                        if (m.containsField("channelNumber"))
                        {
                            int chNo = m.getInt("channelNumber");
                            ci.Number = "" + chNo;
                        }

                        if (m.containsField("services"))
                        {
                            IList tunerInfoList = m.getList("services");
                            HTSMessage firstServiceInList = (HTSMessage)tunerInfoList[0];
                            if (firstServiceInList.containsField("type"))
                            {
                                string type = firstServiceInList.getString("type");

                                switch (type)
                                {
                                    case "Radio":
                                        ci.ChannelType = ChannelType.Radio;
                                        continue;
                                        //break;
                                    case "SDTV":
                                    case "HDTV":
                                        ci.ChannelType = ChannelType.TV;
                                        break;
                                    default:
                                        _logger.Error("[TVHclient] ChannelDataHelper: unkown service type '" + type + "'.");
                                        break;
                                }
                            }
                        }
                        else
                        {
                            _logger.Error("[TVHclient] ChannelDataHelper: unable to detect service-type from service list:" + m.ToString());
                            continue;
                        }
                        result.Add(ci);
                    }
                    return result;
                }
            });
        }
    }
}
