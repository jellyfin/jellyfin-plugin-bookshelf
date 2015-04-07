using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.TVHclient.HTSP;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace MediaBrowser.Plugins.TVHclient.DataHelper
{
    public class AutorecDataHelper
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, HTSMessage> _data;

        private readonly DateTime _initialDateTimeUTC = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public AutorecDataHelper(ILogger logger)
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

        public void autorecEntryAdd(HTSMessage message)
        {
            string id = message.getString("id");
            lock (_data)
            {
                if (_data.ContainsKey(id))
                {
                    _logger.Info("[TVHclient] AutorecDataHelper.autorecEntryAdd id already in database - skip!" + message.ToString());
                    return;
                }
                _data.Add(id, message);
            }
        }

        public void autorecEntryUpdate(HTSMessage message)
        {
            string id = message.getString("id");
            lock (_data)
            {
                HTSMessage oldMessage = _data[id];
                if (oldMessage == null)
                {
                    _logger.Info("[TVHclient] AutorecDataHelper.autorecEntryAdd id not in database - skip!" + message.ToString());
                    return;
                }
                foreach (KeyValuePair<string, object> entry in message)
                {
                    if (oldMessage.containsField(entry.Key))
                    {
                        oldMessage.Remove(entry.Key);
                    }
                    oldMessage.Add(entry.Key, entry.Value);
                }
            }
        }

        public void autorecEntryDelete(HTSMessage message)
        {
            string id = message.getString("id");
            lock (_data)
            {
                _data.Remove(id);
            }
        }

        public Task<IEnumerable<SeriesTimerInfo>> buildAutorecInfos(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew<IEnumerable<SeriesTimerInfo>>(() =>
            {
                lock (_data)
                {
                    List<SeriesTimerInfo> result = new List<SeriesTimerInfo>();

                    // TODO

                    return result;
                }
            });
        }

        public static int getDaysOfWeekFromList(List<DayOfWeek> days)
        {
            int result = 0;
            foreach (DayOfWeek currDay in days)
            {
                switch (currDay)
                {
                    case DayOfWeek.Monday:
                        result = result | 0x1;
                        break;
                    case DayOfWeek.Tuesday:
                        result = result | 0x2;
                        break;
                    case DayOfWeek.Wednesday:
                        result = result | 0x4;
                        break;
                    case DayOfWeek.Thursday:
                        result = result | 0x8;
                        break;
                    case DayOfWeek.Friday:
                        result = result | 0x10;
                        break;
                    case DayOfWeek.Saturday:
                        result = result | 0x20;
                        break;
                    case DayOfWeek.Sunday:
                        result = result | 0x40;
                        break;
                }
            }
            return result;
        }

        public static int getMinutesFromMidnight(DateTime time)
        {
            DateTime utcTime = time.ToUniversalTime();
            int hours = utcTime.Hour;
            int minute = utcTime.Minute;
            int minutes = (hours * 60) + minute;
            return minutes;
        }
    }
}
