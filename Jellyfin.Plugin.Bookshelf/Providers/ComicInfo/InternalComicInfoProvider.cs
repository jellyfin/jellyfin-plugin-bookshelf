using System;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicInfo
{
    /// <summary>
    /// Handles metadata for comics which is saved as an XML document inside of the comic itself.
    /// </summary>
    public class InternalComicInfoProvider : IComicFileProvider
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<InternalComicInfoProvider> _logger;
        private readonly IComicInfoXmlUtilities _utilities = new ComicInfoXmlUtilities();

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalComicInfoProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{InternalComicInfoProvider}"/> interface.</param>
        public InternalComicInfoProvider(IFileSystem fileSystem, ILogger<InternalComicInfoProvider> logger)
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
                _logger.LogInformation("Could not load ComicInfo metadata for {Path} from XML file. No internal XML in comic archive", info.Path);
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
            var file = GetComicBookFile(item.Path);

            if (file is null)
            {
                return false;
            }

            return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
        }

        private async Task<XDocument?> LoadXml(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = GetComicBookFile(info.Path)?.FullName;

            if (path is null)
            {
                return null;
            }

            try
            {
                // Open the comic archive
                using var comicBookFile = ZipFile.OpenRead(path);

                // Try to get the ComicInfo.xml entry
                var container = comicBookFile.GetEntry("ComicInfo.xml");

                if (container is null)
                {
                    return null;
                }

                // Open the xml
                #pragma warning disable CA2007
                await using var containerStream = container.Open();
                #pragma warning restore CA2007

                var comicInfoXml = XDocument.LoadAsync(containerStream, LoadOptions.None, cancellationToken);

                // Read data from XML
                return await comicInfoXml.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not load internal xml from {Path}", path);
                return null;
            }
        }

        private FileSystemMetadata? GetComicBookFile(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.IsDirectory)
            {
                return null;
            }

            // Only parse files that are known to have internal metadata
            if (!string.Equals(fileInfo.Extension, ".cbz", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fileInfo;
        }
    }
}
