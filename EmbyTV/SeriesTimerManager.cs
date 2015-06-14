using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;

namespace EmbyTV
{
    public class SeriesTimerManager : ItemDataProvider<SeriesTimerInfo>
    {
        public SeriesTimerManager(IXmlSerializer xmlSerializer, IJsonSerializer jsonSerializer, ILogger logger, string dataPath)
            : base(xmlSerializer, jsonSerializer, logger, dataPath, (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase))
        {
        }

        protected override string ModifyInputXml(string xml)
        {
            return xml.Replace("<SeriesTimer>", "<SeriesTimerInfo>", StringComparison.OrdinalIgnoreCase).Replace("</SeriesTimer>", "</SeriesTimerInfo>", StringComparison.OrdinalIgnoreCase);
        }

        public override void Add(SeriesTimerInfo item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                throw new ArgumentException("SeriesTimerInfo.Id cannot be null or empty.");
            }

            base.Add(item);
        }
    }
}
