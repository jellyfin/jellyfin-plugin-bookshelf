using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using System;
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
    public class AlbumXmlSaver : IMetadataSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataRepository _userDataRepo;

        public AlbumXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataRepository userDataRepo)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepo = userDataRepo;
        }

        public string GetSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "album.nfo");
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<album>");

            XmlSaverHelpers.AddCommonNodes(item, builder, _libraryManager, _userManager, _userDataRepo);

            var tracks = ((MusicAlbum)item).Children.OfType<Audio>().ToList();

            var artists = tracks
                .SelectMany(i =>
                {
                    var list = new List<string>();

                    if (!string.IsNullOrEmpty(i.AlbumArtist))
                    {
                        list.Add(i.AlbumArtist);
                    }
                    list.AddRange(i.Artists);

                    return list;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var artist in artists)
            {
                builder.Append("<artist>" + SecurityElement.Escape(artist) + "</artist>");
            }

            AddTracks(tracks, builder);

            builder.Append("</album>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "track"
                });
        }

        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            // If new metadata has been downloaded or metadata was manually edited, proceed
            if ((updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload
                || (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit)
            {
                return item is MusicAlbum;
            }

            return false;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private void AddTracks(IEnumerable<Audio> tracks, StringBuilder builder)
        {
            foreach (var track in tracks)
            {
                builder.Append("<track>");

                if (track.IndexNumber.HasValue)
                {
                    builder.Append("<position>" + SecurityElement.Escape(track.IndexNumber.Value.ToString(UsCulture)) + "</position>");
                }

                if (!string.IsNullOrEmpty(track.Name))
                {
                    builder.Append("<title>" + SecurityElement.Escape(track.Name) + "</title>");
                }

                if (track.RunTimeTicks.HasValue)
                {
                    var time = TimeSpan.FromTicks(track.RunTimeTicks.Value).ToString(@"mm\:ss");

                    builder.Append("<duration>" + SecurityElement.Escape(time) + "</duration>");
                }

                builder.Append("</track>");
            }
        }
    }
}
