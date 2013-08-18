using System.Threading.Tasks;
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
        private readonly ILibraryManager _libraryManager;
        
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public EpisodeXmlSaver(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public string GetSavePath(BaseItem item)
        {
            return Path.ChangeExtension(item.Path, ".nfo");
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<episodedetails>");

            var task = XmlSaverHelpers.AddCommonNodes(item, builder, _libraryManager);

            Task.WaitAll(task);

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

            XmlSaverHelpers.AddMediaInfo((Episode)item, builder);

            builder.Append("</episodedetails>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new string[]
                {
                    "aired",
                    "season",
                    "episode"
                });
        }

        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            // If new metadata has been downloaded or metadata was manually edited, proceed
            if ((updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload
                || (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit)
            {
                return item is Episode;
            }

            return false;
        }
    }
}
