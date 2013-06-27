using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata.Savers
{
    public class SeriesXmlSaver : IMetadataSaver
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IServerConfigurationManager _config;

        public SeriesXmlSaver(IServerConfigurationManager config)
        {
            _config = config;
        }

        public string GetSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "tvshow.nfo");
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<tvshow>");

            XmlSaverHelpers.AddCommonNodes(item, builder);

            var tvdb = item.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(tvdb))
            {
                builder.Append("<id>" + SecurityElement.Escape(tvdb) + "</id>");

                builder.AppendFormat("<episodeguide><url cache=\"{0}.xml\">http://www.thetvdb.com/api/1D62F2F90030C444/series/{0}/all/{1}.zip</url></episodeguide>", 
                    tvdb,
                    string.IsNullOrEmpty(_config.Configuration.PreferredMetadataLanguage) ? "en" : _config.Configuration.PreferredMetadataLanguage);
            }

            var imdb = item.GetProviderId(MetadataProviders.Imdb);

            if (!string.IsNullOrEmpty(imdb))
            {
                builder.Append("<imdb_id>" + SecurityElement.Escape(imdb) + "</imdb_id>");
            }

            builder.Append("<season>-1</season>");
            builder.Append("<episode>-1</episode>");

            var series = (Series)item;

            if (series.Status.HasValue)
            {
                builder.Append("<status>" + SecurityElement.Escape(series.Status.Value.ToString()) + "</status>");
            }

            if (series.Studios.Count > 0)
            {
                builder.Append("<studio>" + SecurityElement.Escape(item.Studios[0]) + "</studio>");
            }

            if (!string.IsNullOrEmpty(series.AirTime))
            {
                builder.Append("<airs_time>" + SecurityElement.Escape(series.AirTime) + "</airs_time>");
            }

            if (series.AirDays.Count == 7)
            {
                builder.Append("<airs_dayofweek>" + SecurityElement.Escape("Daily") + "</airs_dayofweek>");
            }
            else if (series.AirDays.Count > 0)
            {
                builder.Append("<airs_dayofweek>" + SecurityElement.Escape(series.AirDays[0].ToString()) + "</airs_dayofweek>");
            }
            
            builder.Append("</tvshow>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new[]
                {
                    "id",
                    "imdb_id",
                    "season",
                    "episode",
                    "status",
                    "studio",
                    "airs_time",
                    "airs_dayofweek",
                    "episodeguide"
                });
        }

        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            // If new metadata has been downloaded or metadata was manually edited, proceed
            if ((updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload
                || (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit)
            {
                return item is Series;
            }

            return false;
        }
    }
}
