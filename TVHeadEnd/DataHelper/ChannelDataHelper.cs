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

        public ChannelDataHelper(ILogger logger) : this(logger, null) {}

        public void SetChannelType4Other(string channelType4Other)
        {
            _channelType4Other = channelType4Other;
        }

        public void Clean()
        {
            lock (_data)
            {
                _data.Clear();
                if (_tunerDataHelper != null)
                {
                    _tunerDataHelper.clean();
                }
            }
        }

        public void Add(HTSMessage message)
        {
            if (_tunerDataHelper != null)
            {
                // TVHeadend don't send the information we need 
                // _tunerDataHelper.addTunerInfo(message);
            }

            lock (_data)
            {
                try
                {
                    int channelID = message.getInt("channelId");
                    if (_data.ContainsKey(channelID))
                    {
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
                            _data.Add(channelID, message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("[TVHclient] ChannelDataHelper.Add caught exception: " + ex.Message + "\nHTSmessage=" + message);
                }
            }
        }

        public string GetChannelIcon4ChannelId(string channelId)
        {
            return _piconData[channelId];
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

                        try
                        {
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
                                else
                                {
                                    ci.HasImage = true;
                                    if(!_piconData.ContainsKey(ci.Id))
                                    {
                                        _piconData.Add(ci.Id, channelIcon);
                                    }
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
                                int channelNumber = m.getInt("channelNumber");
                                ci.Number = "" + channelNumber;
                                if (m.containsField("channelNumberMinor"))
                                {
                                    int channelNumberMinor = m.getInt("channelNumberMinor");
                                    ci.Number = ci.Number + "." + channelNumberMinor;
                                }
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
                        catch (Exception ex)
                        {
                            _logger.Error("[TVHclient] ChannelDataHelper.BuildChannelInfos caught exception: " + ex.Message + "\nHTSmessage=" + m);
                        }
                    }
                    return result;
                }
            });
        }
    }
}
