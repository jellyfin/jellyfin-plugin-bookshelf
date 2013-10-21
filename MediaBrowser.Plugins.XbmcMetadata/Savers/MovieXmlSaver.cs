using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata.Savers
{
    public class MovieXmlSaver : IMetadataSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepo;

        public MovieXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepo = userDataRepo;
        }

        public string GetSavePath(BaseItem item)
        {
            if (item.ResolveArgs.IsDirectory)
            {
                var video = (Video)item;

                if (video.VideoType == VideoType.Dvd || video.VideoType == VideoType.BluRay || video.VideoType == VideoType.HdDvd)
                {
                    var path = item.Path;

                    return Path.Combine(path, Path.GetFileNameWithoutExtension(path) + ".nfo");
                }
            }

            return Path.ChangeExtension(item.Path, ".nfo");
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            var tag = item is MusicVideo ? "musicvideo" : "movie";

            builder.Append("<" + tag + ">");

            XmlSaverHelpers.AddCommonNodes(item, builder, _libraryManager, _userManager, _userDataRepo);

            var imdb = item.GetProviderId(MetadataProviders.Imdb);

            if (!string.IsNullOrEmpty(imdb))
            {
                builder.Append("<id>" + SecurityElement.Escape(imdb) + "</id>");
            }

            var musicVideo = item as MusicVideo;

            if (musicVideo != null)
            {
                if (!string.IsNullOrEmpty(musicVideo.Artist))
                {
                    builder.Append("<artist>" + SecurityElement.Escape(musicVideo.Artist) + "</artist>");
                }
                if (!string.IsNullOrEmpty(musicVideo.Album))
                {
                    builder.Append("<album>" + SecurityElement.Escape(musicVideo.Album) + "</album>");
                }
            }

            var movie = item as Movie;

            if (movie != null)
            {
                if (!string.IsNullOrEmpty(movie.TmdbCollectionName))
                {
                    builder.Append("<set>" + SecurityElement.Escape(movie.TmdbCollectionName) + "</set>");
                }
            }
            
            XmlSaverHelpers.AddMediaInfo((Video)item, builder);

            builder.Append("</" + tag + ">");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "album",
                    "artist",
                    "set"
                });
        }

        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            // If new metadata has been downloaded or metadata was manually edited, proceed
            if ((updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload
                || (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit)
            {
                var trailer = item as Trailer;

                if (trailer != null)
                {
                    return !trailer.IsLocalTrailer;
                }

                return item is Movie || item is MusicVideo;
            }

            return false;
        }
    }
}
