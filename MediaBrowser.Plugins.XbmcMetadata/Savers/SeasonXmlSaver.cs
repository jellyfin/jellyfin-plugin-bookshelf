using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.XbmcMetadata.Savers
{
    public class SeasonXmlSaver : IMetadataSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataRepository _userDataRepo;

        public SeasonXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataRepository userDataRepo)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepo = userDataRepo;
        }

        public string GetSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "season.nfo");
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<season>");

            XmlSaverHelpers.AddCommonNodes(item, builder, _libraryManager, _userManager, _userDataRepo);

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
