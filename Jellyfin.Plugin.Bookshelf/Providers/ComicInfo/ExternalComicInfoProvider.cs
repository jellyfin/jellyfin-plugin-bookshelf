using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicInfo
{
    /// <summary>
    /// Handles metadata for comics which is saved as an XML document. This XML document is not part
    /// of the comic itself, but an external file.
    /// </summary>
    public class ExternalComicInfoProvider : IComicFileProvider
    {
        private const string ComicRackMetaFile = "ComicInfo.xml";

        private readonly IFileSystem _fileSystem;
        private readonly ILogger<ExternalComicInfoProvider> _logger;
        private readonly ComicInfoXmlUtilities _utilities = new ComicInfoXmlUtilities();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalComicInfoProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{ExternalComicInfoProvider}"/> interface.</param>
        public ExternalComicInfoProvider(IFileSystem fileSystem, ILogger<ExternalComicInfoProvider> logger)
        {
            _logger = logger;
            _fileSystem = fileSystem;
        }

        /// <inheritdoc />
        public async ValueTask<MetadataResult<Book>> ReadMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var comicInfoXml = await LoadXml(info, directoryService, cancellationToken).ConfigureAwait(false);

            if (comicInfoXml is null)
            {
                _logger.LogInformation("Could not load ComicInfo metadata for {Path} from XML file", info.Path);
                return new MetadataResult<Book> { HasMetadata = false };
            }

            var book = _utilities.ReadComicBookMetadata(comicInfoXml);

            // If we found no metadata about the book itself, abort mission
            if (book is null)
            {
                return new MetadataResult<Book> { HasMetadata = false };
            }

            var metadataResult = new MetadataResult<Book> { Item = book, HasMetadata = true };

            // We have found metadata like the title
            // let's search for the people like the author and save the found metadata
            _utilities.ReadPeopleMetadata(comicInfoXml, metadataResult);

            _utilities.ReadCultureInfoInto(comicInfoXml, "ComicInfo/LanguageISO", cultureInfo => metadataResult.ResultLanguage = cultureInfo.ThreeLetterISOLanguageName);

            return metadataResult;
        }

        /// <inheritdoc />
        public bool HasItemChanged(BaseItem item)
        {
            var file = GetXmlFilePath(item.Path);
            return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
        }

        private async Task<XDocument?> LoadXml(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = GetXmlFilePath(info.Path).FullName;

            if (path is null)
            {
                return null;
            }

            try
            {
                // Open the xml
                using var reader = XmlReader.Create(path, new XmlReaderSettings { Async = true });

                var comicInfoXml = XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);

                // Read data from XML
                return await comicInfoXml.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, "Could not load external xml from {Path}. This could mean there is no separate ComicInfo metadata file for this comic. Maybe the metadata is bundled within the comic", path);
                return null;
            }
        }

        private FileSystemMetadata GetXmlFilePath(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            var directoryInfo = fileInfo.IsDirectory ? fileInfo : _fileSystem.GetDirectoryInfo(Path.GetDirectoryName(path)!);

            var directoryPath = directoryInfo.FullName;

            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(path) + ".xml");

            var file = _fileSystem.GetFileInfo(specificFile);

            return file.Exists ? file : _fileSystem.GetFileInfo(Path.Combine(directoryPath, ComicRackMetaFile));
        }
    }
}
