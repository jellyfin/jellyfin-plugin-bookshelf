using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata.Savers
{
    public class SeasonXmlSaver : IMetadataFileSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepo;

        public SeasonXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepo = userDataRepo;
        }

        public string Name
        {
            get
            {
                return "Xbmc nfo";
            }
        }

        public string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "season.nfo");
        }

        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<season>");

            XmlSaverHelpers.AddCommonNodes((Season)item, builder, _libraryManager, _userManager, _userDataRepo);

            builder.Append("</season>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string> { });
        }

        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
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
