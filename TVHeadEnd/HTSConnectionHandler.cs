using MediaBrowser.Model.Logging;
using System;
using System.Reflection;
using System.Threading;
using TVHeadEnd.HTSP;
using TVHeadEnd.DataHelper;
using System.Threading.Tasks;
using System.Collections.Generic;
using MediaBrowser.Controller.LiveTv;

namespace TVHeadEnd
{
    class HTSConnectionHandler : HTSConnectionListener
    {
        private static volatile HTSConnectionHandler _instance;
        private static object _syncRoot = new Object();

        private readonly object _lock = new Object();

        private readonly ILogger _logger;

        private volatile Boolean _initialLoadFinished = false;
        private volatile Boolean _connected = false;

        private HTSConnectionAsync _htsConnection;
        private int _priority;
        private string _profile;
        private string _httpBaseUrl;
        private string _channelType;
        private string _tvhServerName;
        private int _httpPort;
        private int _htspPort;
        private string _userName;
        private string _password;
        private bool _enableSubsMaudios;

        // Data helpers
        private readonly ChannelDataHelper _channelDataHelper;
        private readonly DvrDataHelper _dvrDataHelper;
        private readonly AutorecDataHelper _autorecDataHelper;

        private LiveTvService _liveTvService;

        private HTSConnectionHandler(ILogger logger)
        {
            _logger = logger;

            //System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
            _logger.Info("[TVHclient] HTSConnectionHandler()");

            _channelDataHelper = new ChannelDataHelper(logger);
            _dvrDataHelper = new DvrDataHelper(logger);
            _autorecDataHelper = new AutorecDataHelper(logger);

            init();
            _channelDataHelper.SetChannelType4Other(_channelType);
        }

        public static HTSConnectionHandler GetInstance(ILogger logger)
        {
            if (_instance == null)
            {
                lock (_syncRoot)
                {
                    if (_instance == null)
                    {
                        _instance = new HTSConnectionHandler(logger);
                    }
                }
            }
            return _instance;
        }

        public void setLiveTvService(LiveTvService liveTvService)
        {
            _liveTvService = liveTvService;
        }

        public int WaitForInitialLoad(CancellationToken cancellationToken)
        {
            ensureConnection();
            DateTime start = DateTime.Now;
            while (!_initialLoadFinished || cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(500);
                TimeSpan duration = DateTime.Now - start;
                long durationInSec = duration.Ticks / TimeSpan.TicksPerSecond;
                if (durationInSec > 60 * 15) // 15 Min timeout, should be enough to load huge data count
                {
                    return -1;
                }
            }
            return 0;
        }

        private void init()
        {
            var config = Plugin.Instance.Configuration;

            if (string.IsNullOrEmpty(config.TVH_ServerName))
            {
                string message = "[TVHclient] HTSConnectionHandler.ensureConnection: TVH-Server name must be configured.";
                _logger.Error(message);
                throw new InvalidOperationException(message);
            }

            if (string.IsNullOrEmpty(config.Username))
            {
                string message = "[TVHclient] HTSConnectionHandler.ensureConnection: Username must be configured.";
                _logger.Error(message);
                throw new InvalidOperationException(message);
            }

            if (string.IsNullOrEmpty(config.Password))
            {
                string message = "[TVHclient] HTSConnectionHandler.ensureConnection: Password must be configured.";
                _logger.Error(message);
                throw new InvalidOperationException(message);
            }

            _priority = config.Priority;
            _profile = config.Profile.Trim();
            _channelType = config.ChannelType.Trim();
            _enableSubsMaudios = config.EnableSubsMaudios;

            if (_priority < 0 || _priority > 4)
            {
                _priority = 2;
                _logger.Info("[TVHclient] HTSConnectionHandler.ensureConnection: Priority was out of range [0-4] - set to 2");
            }

            _tvhServerName = config.TVH_ServerName.Trim();
            _httpPort = config.HTTP_Port;
            _htspPort = config.HTSP_Port;
            _userName = config.Username.Trim();
            _password = config.Password.Trim();

            if (_enableSubsMaudios)
            {
            
                // Use HTTP basic auth instead of TVH ticketing system for authentication to allow the users to switch subs or audio tracks at any time
                _httpBaseUrl = "http://" + _userName + ":" + _password + "@" + _tvhServerName + ":" + _httpPort;

            }
            else
            {

                _httpBaseUrl = "http://" + _tvhServerName + ":" + _httpPort;

            }
        }

        private void ensureConnection()
        {
            _logger.Info("[TVHclient] HTSConnectionHandler.ensureConnection()");
            if (_htsConnection == null || _htsConnection.needsRestart())
            {
                _logger.Info("[TVHclient] HTSConnectionHandler.ensureConnection() : create new HTS-Connection");
                Version version = Assembly.GetEntryAssembly().GetName().Version;
                _htsConnection = new HTSConnectionAsync(this, "TVHclient4Emby", version.ToString(), _logger);
                _connected = false;
            }

            lock (_lock)
            {
                if (!_connected)
                {
                    _logger.Info("[TVHclient] HTSConnectionHandler.ensureConnection: Used connection parameters: " +
                        "TVH Server = '" + _tvhServerName + "'; " +
                        "HTTP Port = '" + _httpPort + "'; " +
                        "HTSP Port = '" + _htspPort + "'; " +
                        "User = '" + _userName + "'; " +
                        "Password set = '" + (_password.Length > 0) + "'");

                    _htsConnection.open(_tvhServerName, _htspPort);
                    _connected = _htsConnection.authenticate(_userName, _password);

                    _logger.Info("[TVHclient] HTSConnectionHandler.ensureConnection: connection established " + _connected);
                }
            }
        }

        public void SendMessage(HTSMessage message, HTSResponseHandler responseHandler)
        {
            ensureConnection();
            _htsConnection.sendMessage(message, responseHandler);
        }

        public String GetServername()
        {
            ensureConnection();
            return _htsConnection.getServername();
        }

        public String GetServerVersion()
        {
            ensureConnection();
            return _htsConnection.getServerversion();
        }

        public int GetServerProtocolVersion()
        {
            ensureConnection();
            return _htsConnection.getServerProtocolVersion();
        }

        public String GetDiskSpace()
        {
            ensureConnection();
            return _htsConnection.getDiskspace();
        }

        public Task<IEnumerable<ChannelInfo>> BuildChannelInfos(CancellationToken cancellationToken)
        {
            return _channelDataHelper.BuildChannelInfos(cancellationToken);
        }

        public int GetPriority()
        {
            return _priority;
        }

        public String GetProfile()
        {
            return _profile;
        }

        public String GetPiconData(String channelID)
        {
            return _channelDataHelper.GetPiconData(channelID);
        }

        public String GetHttpBaseUrl()
        {
            return _httpBaseUrl;
        }

        public bool GetEnableSubsMaudios()
        {
            return _enableSubsMaudios;
        }

        public Task<IEnumerable<RecordingInfo>> BuildDvrInfos(CancellationToken cancellationToken)
        {
            return _dvrDataHelper.buildDvrInfos(cancellationToken);
        }

        public Task<IEnumerable<SeriesTimerInfo>> BuildAutorecInfos(CancellationToken cancellationToken)
        {
            return _autorecDataHelper.buildAutorecInfos(cancellationToken);
        }

        public Task<IEnumerable<TimerInfo>> BuildPendingTimersInfos(CancellationToken cancellationToken)
        {
            return _dvrDataHelper.buildPendingTimersInfos(cancellationToken);
        }

        public void onError(Exception ex)
        {
            _logger.ErrorException("[TVHclient] HTSConnectionHandler recorded a HTSP error: " + ex.Message, ex);
            _htsConnection.stop();
            _htsConnection = null;
            _connected = false;

            _liveTvService.sendDataSourceChanged();

            ensureConnection();

            throw ex;
        }

        public void onMessage(HTSMessage response)
        {
            if (response != null)
            {
                switch (response.Method)
                {
                    case "tagAdd":
                    case "tagUpdate":
                    case "tagDelete":
                        //_logger.Fatal("[TVHclient] tad add/update/delete" + response.ToString());
                        break;

                    case "channelAdd":
                    case "channelUpdate":
                        _channelDataHelper.Add(response);
                        break;

                    case "dvrEntryAdd":
                        _dvrDataHelper.dvrEntryAdd(response);
                        break;
                    case "dvrEntryUpdate":
                        _dvrDataHelper.dvrEntryUpdate(response);
                        break;
                    case "dvrEntryDelete":
                        _dvrDataHelper.dvrEntryDelete(response);
                        break;

                    case "autorecEntryAdd":
                        _autorecDataHelper.autorecEntryAdd(response);
                        break;
                    case "autorecEntryUpdate":
                        _autorecDataHelper.autorecEntryUpdate(response);
                        break;
                    case "autorecEntryDelete":
                        _autorecDataHelper.autorecEntryDelete(response);
                        break;

                    case "eventAdd":
                    case "eventUpdate":
                    case "eventDelete":
                        // should not happen as we don't subscribe for this events.
                        break;

                    //case "subscriptionStart":
                    //case "subscriptionGrace":
                    //case "subscriptionStop":
                    //case "subscriptionSkip":
                    //case "subscriptionSpeed":
                    //case "subscriptionStatus":
                    //    _logger.Fatal("[TVHclient] subscription events " + response.ToString());
                    //    break;

                    //case "queueStatus":
                    //    _logger.Fatal("[TVHclient] queueStatus event " + response.ToString());
                    //    break;

                    //case "signalStatus":
                    //    _logger.Fatal("[TVHclient] signalStatus event " + response.ToString());
                    //    break;

                    //case "timeshiftStatus":
                    //    _logger.Fatal("[TVHclient] timeshiftStatus event " + response.ToString());
                    //    break;

                    //case "muxpkt": // streaming data
                    //    _logger.Fatal("[TVHclient] muxpkt event " + response.ToString());
                    //    break;

                    case "initialSyncCompleted":
                        _initialLoadFinished = true;
                        break;

                    default:
                        //_logger.Fatal("[TVHclient] Method '" + response.Method + "' not handled in LiveTvService.cs");
                        break;
                }
            }
        }
    }
}
