using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using System;
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
        private readonly Dictionary<string, string> _piconData;
        private string _channelType4Other = "Ignore";

        public ChannelDataHelper(ILogger logger, TunerDataHelper tunerDataHelper)
        {
            _logger = logger;
            _tunerDataHelper = tunerDataHelper;

            _data = new Dictionary<int, HTSMessage>();
            _piconData = new Dictionary<string, string>();
        }

        public ChannelDataHelper(ILogger logger)
        {
            _logger = logger;
            _tunerDataHelper = null;

            _data = new Dictionary<int, HTSMessage>();
            _piconData = new Dictionary<string, string>();
        }

        public void SetChannelType4Other(string channelType4Other)
        {
            _channelType4Other = channelType4Other;
        }

        public void Clean()
        {
            lock (_data)
            {
                _data.Clear();
                _tunerDataHelper.clean();
            }
        }

        public void Add(HTSMessage message)
        {
            // TVHeadend don't send the information we need 
            //_tunerDataHelper.addTunerInfo(message);

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

        public string GetPiconData(string channelID)
        {
            if (_piconData.ContainsKey(channelID))
            {
                return _piconData[channelID];
            }
            else
            {
                return null;
            }
        }

        public Task<IEnumerable<ChannelInfo>> BuildChannelInfos(CancellationToken cancellationToken)
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
                        {
                            string channelIcon = m.getString("channelIcon");
                            Uri uriResult;
                            bool uriCheckResult = Uri.TryCreate(channelIcon, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp;
                            if (uriCheckResult)
                            {
                                ci.ImageUrl = channelIcon;
                            }
                            else if (channelIcon.ToLower().StartsWith("picon://"))
                            {
                                ci.HasImage = true;
                                _piconData.Add(ci.Id, channelIcon);
                            }
                            else
                            {
                                _logger.Info("[TVHclient] ChannelDataHelper.buildChannelInfos: channelIcon '" + channelIcon +
                                    "' can not be handled properly for channelID '" + ci.Id + "'!");
                            }
                        }
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

                        Boolean serviceFound = false;
                        if (m.containsField("services"))
                        {
                            IList tunerInfoList = m.getList("services");
                            if (tunerInfoList != null && tunerInfoList.Count > 0)
                            {
                                HTSMessage firstServiceInList = (HTSMessage)tunerInfoList[0];
                                if (firstServiceInList.containsField("type"))
                                {
                                    string type = firstServiceInList.getString("type").ToLower();
                                    switch (type)
                                    {
                                        case "radio":
                                            ci.ChannelType = ChannelType.Radio;
                                            serviceFound = true;
                                            break;
                                        case "sdtv":
                                        case "hdtv":
                                            ci.ChannelType = ChannelType.TV;
                                            serviceFound = true;
                                            break;
                                        case "other":
                                            switch (_channelType4Other.ToLower())
                                            {
                                                case "tv":
                                                    _logger.Info("[TVHclient] ChannelDataHelper: map service tag 'Other' to 'TV'.");
                                                    ci.ChannelType = ChannelType.TV;
                                                    serviceFound = true;
                                                    break;
                                                case "radio":
                                                    _logger.Info("[TVHclient] ChannelDataHelper: map service tag 'Other' to 'Radio'.");
                                                    ci.ChannelType = ChannelType.Radio;
                                                    serviceFound = true;
                                                    break;
                                                default:
                                                    _logger.Info("[TVHclient] ChannelDataHelper: don't map service tag 'Other' - will be ignored.");
                                                    break;
                                            }
                                            break;
                                        default:
                                            _logger.Info("[TVHclient] ChannelDataHelper: unkown service tag '" + type + "' - will be ignored.");
                                            break;
                                    }
                                }
                            }
                        }
                        if (!serviceFound)
                        {
                            _logger.Info("[TVHclient] ChannelDataHelper: unable to detect service-type (tvheadend tag!!!) from service list:" + m.ToString());
                            continue;
                        }

                        _logger.Info("[TVHclient] ChannelDataHelper: Adding channel \n" + m.ToString());

                        result.Add(ci);
                    }
                    return result;
                }
            });
        }
    }
}
