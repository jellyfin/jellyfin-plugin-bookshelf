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
    public class EpisodeXmlSaver : IMetadataSaver
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        
        public string GetSavePath(BaseItem item)
        {
            return Path.ChangeExtension(item.Path, ".nfo");
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<episodedetails>");

            XmlSaverHelpers.AddCommonNodes(item, builder);

            if (item.IndexNumber.HasValue)
            {
                builder.Append("<episode>" + item.IndexNumber.Value.ToString(_usCulture) + "</episode>");
            }

            if (item.ParentIndexNumber.HasValue)
            {
                builder.Append("<season>" + item.ParentIndexNumber.Value.ToString(_usCulture) + "</season>");
            }

            if (item.PremiereDate.HasValue)
            {
                builder.Append("<aired>" + SecurityElement.Escape(item.PremiereDate.Value.ToShortDateString()) + "</aired>");
            }

            builder.Append("</episodedetails>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new string[] { });
        }

        public bool Supports(BaseItem item)
        {
            return item is Episode && item.LocationType == LocationType.FileSystem;
        }
    }
}
