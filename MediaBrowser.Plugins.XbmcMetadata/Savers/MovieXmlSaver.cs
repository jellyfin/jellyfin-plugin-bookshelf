using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata.Savers
{
    public class MovieXmlSaver : IMetadataSaver
    {
        private readonly ILibraryManager _libraryManager;

        public MovieXmlSaver(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public string GetSavePath(BaseItem item)
        {
            if (item.ResolveArgs.IsDirectory)
            {
                var video = (Video)item;
                var path = video.VideoType == VideoType.VideoFile || video.VideoType == VideoType.Iso ? Path.GetDirectoryName(item.Path) : item.Path;

                if (item is MusicVideo)
                {
                    return Path.Combine(path, Path.GetFileName(path) + ".nfo");
                }
                return Path.Combine(path, "movie.nfo");
            }

            return Path.ChangeExtension(item.Path, ".nfo");
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            var tag = item is MusicVideo ? "musicvideo" : "movie";

            builder.Append("<" + tag + ">");

            var task = XmlSaverHelpers.AddCommonNodes(item, builder, _libraryManager);

            Task.WaitAll(task);

            var imdb = item.GetProviderId(MetadataProviders.Imdb);

            if (!string.IsNullOrEmpty(imdb))
            {
                builder.Append("<id>" + SecurityElement.Escape(imdb) + "</id>");
            }

            XmlSaverHelpers.AddMediaInfo((Video)item, builder);

            builder.Append("</" + tag + ">");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new[]
                {
                    "id"
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
