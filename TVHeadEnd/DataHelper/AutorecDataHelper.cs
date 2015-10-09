using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TVHeadEnd.HTSP;


namespace TVHeadEnd.DataHelper
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
                        oldMessage.removeField(entry.Key);
                    }
                    oldMessage.putField(entry.Key, entry.Value);
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

                    foreach (KeyValuePair<string, HTSMessage> entry in _data)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.Info("[TVHclient] DvrDataHelper.buildDvrInfos, call canceled - returning part list.");
                            return result;
                        }

                        HTSMessage m = entry.Value;
                        SeriesTimerInfo sti = new SeriesTimerInfo();

                        try
                        {
                            if (m.containsField("id"))
                            {
                                sti.Id = m.getString("id");
                            }
                        }
                        catch (InvalidCastException)
                        {
                        }

                        try
                        {
                            if (m.containsField("daysOfWeek"))
                            {
                                int daysOfWeek = m.getInt("daysOfWeek");
                                sti.Days = getDayOfWeekListFromInt(daysOfWeek);
                            }
                        }
                        catch (InvalidCastException)
                        {
                        }

                        sti.StartDate = DateTime.Now.ToUniversalTime();

                        try
                        {
                            if (m.containsField("retention"))
                            {
                                int retentionInDays = m.getInt("retention");
                                sti.EndDate = DateTime.Now.AddDays(retentionInDays).ToUniversalTime();
                            }
                        }
                        catch (InvalidCastException)
                        {
                        }

                        try
                        {
                            if (m.containsField("channel"))
                            {
                                sti.ChannelId = "" + m.getInt("channel");
                            }
                        }
                        catch (InvalidCastException)
                        {
                        }

                        try
                        {
                            if (m.containsField("startExtra"))
                            {
                                sti.PrePaddingSeconds = (int)m.getLong("startExtra") * 60;
                                sti.IsPrePaddingRequired = true;
                            }
                        }
                        catch (InvalidCastException)
                        {
                        }

                        try
                        {
                            if (m.containsField("stopExtra"))
                            {
                                sti.PostPaddingSeconds = (int)m.getLong("stopExtra") * 60;
                                sti.IsPostPaddingRequired = true;
                            }
                        }
                        catch (InvalidCastException)
                        {
                        }

                        try
                        {
                            if (m.containsField("title"))
                            {
                                sti.Name = m.getString("title");
                            }
                        }
                        catch (InvalidCastException)
                        {
                        }

                        try
                        {
                            if (m.containsField("description"))
                            {
                                sti.Overview = m.getString("description");
                            }
                        }
                        catch (InvalidCastException)
                        {
                        }

                        try
                        {
                            if (m.containsField("priority"))
                            {
                                sti.Priority = m.getInt("priority");
                            }
                        }
                        catch (InvalidCastException)
                        {
                        }

                        /*
                                public string ProgramId { get; set; }
                                public bool RecordAnyChannel { get; set; }
                                public bool RecordAnyTime { get; set; }
                                public bool RecordNewOnly { get; set; }
                         */

                        result.Add(sti);
                    }

                    return result;
                }
            });
        }

        private List<DayOfWeek> getDayOfWeekListFromInt(int daysOfWeek)
        {
            List<DayOfWeek> result = new List<DayOfWeek>();
            if ((daysOfWeek & 0x01) != 0)
            {
                result.Add(DayOfWeek.Monday);
            }
            if ((daysOfWeek & 0x02) != 0)
            {
                result.Add(DayOfWeek.Tuesday);
            }
            if ((daysOfWeek & 0x04) != 0)
            {
                result.Add(DayOfWeek.Wednesday);
            }
            if ((daysOfWeek & 0x08) != 0)
            {
                result.Add(DayOfWeek.Thursday);
            }
            if ((daysOfWeek & 0x10) != 0)
            {
                result.Add(DayOfWeek.Friday);
            }
            if ((daysOfWeek & 0x20) != 0)
            {
                result.Add(DayOfWeek.Saturday);
            }
            if ((daysOfWeek & 0x40) != 0)
            {
                result.Add(DayOfWeek.Sunday);
            }
            return result;
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
