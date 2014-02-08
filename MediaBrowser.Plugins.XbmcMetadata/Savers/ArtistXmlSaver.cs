using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Plugins.XbmcMetadata.Savers
{
    public class ArtistXmlSaver : IMetadataFileSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepo;

        public string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "artist.nfo");
        }

        public string Name
        {
            get
            {
                return "Xbmc nfo";
            }
        }

        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var artist = (MusicArtist)item;

            var builder = new StringBuilder();

            builder.Append("<artist>");

            XmlSaverHelpers.AddCommonNodes(artist, builder, _libraryManager, _userManager, _userDataRepo);

            if (artist.EndDate.HasValue)
            {
                var formatString = Plugin.Instance.Configuration.ReleaseDateFormat;

                if (item is MusicArtist)
                {
                    builder.Append("<disbanded>" + SecurityElement.Escape(artist.EndDate.Value.ToString(formatString)) + "</disbanded>");
                }
            }

            var albums = artist
                .RecursiveChildren
                .OfType<MusicAlbum>()
                .ToList();

            AddAlbums(albums, builder);

            builder.Append("</artist>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "album",
                    "disbanded"
                });
        }

        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            var locationType = item.LocationType;
            if (locationType == LocationType.Remote || locationType == LocationType.Virtual)
            {
                return false;
            }

            // If new metadata has been downloaded or metadata was manually edited, proceed
            if ((updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload
                || (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit)
            {
                return item is MusicArtist;
            }

            return false;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public ArtistXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo)
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
