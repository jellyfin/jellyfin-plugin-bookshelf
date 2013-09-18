using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.XbmcMetadata.Savers
{
    public class ArtistXmlSaver : IMetadataSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataRepository _userDataRepo;

        public string GetSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "artist.nfo");
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<artist>");

            XmlSaverHelpers.AddCommonNodes(item, builder, _libraryManager, _userManager, _userDataRepo);

            var albums = ((MusicArtist)item).Children.OfType<MusicAlbum>().ToList();

            AddAlbums(albums, builder);

            builder.Append("</artist>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new[]
                {
                    "album"
                });
        }

        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            // If new metadata has been downloaded or metadata was manually edited, proceed
            if ((updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload
                || (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit)
            {
                return item is MusicArtist;
            }

            return false;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public ArtistXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataRepository userDataRepo)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepo = userDataRepo;
        }

        private void AddAlbums(IEnumerable<MusicAlbum> albums, StringBuilder builder)
        {
            foreach (var album in albums)
            {
                builder.Append("<album>");

                if (!string.IsNullOrEmpty(album.Name))
                {
                    builder.Append("<title>" + SecurityElement.Escape(album.Name) + "</title>");
                }

                if (album.ProductionYear.HasValue)
                {
                    builder.Append("<year>" + SecurityElement.Escape(album.ProductionYear.Value.ToString(UsCulture)) + "</year>");
                }

                builder.Append("</album>");
            }
        }
    }
}
