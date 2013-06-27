using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.IO;
using System.Text;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata.Savers
{
    public class SeasonXmlSaver : IMetadataSaver
    {
        public string GetSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "season.nfo");
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<season>");

            XmlSaverHelpers.AddCommonNodes(item, builder);

            builder.Append("</season>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new string[] { });
        }

        public bool Supports(BaseItem item)
        {
            return item is Season && item.LocationType == LocationType.FileSystem;
        }
    }
}
