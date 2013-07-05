using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using System.IO;
using System.Text;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata.Savers
{
    public class SeasonXmlSaver : IMetadataSaver
    {
        private readonly ILibraryManager _libraryManager;

        public SeasonXmlSaver(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public string GetSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "season.nfo");
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<season>");

            var task = XmlSaverHelpers.AddCommonNodes(item, builder, _libraryManager);

            Task.WaitAll(task);

            builder.Append("</season>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new string[] { });
        }

        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            // If new metadata has been downloaded or metadata was manually edited, proceed
            if ((updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload
                || (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit)
            {
                return item is Season;
            }

            return false;
        }
    }
}
