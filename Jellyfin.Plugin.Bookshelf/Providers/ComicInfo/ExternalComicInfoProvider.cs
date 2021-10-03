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

#nullable enable
namespace Jellyfin.Plugin.Bookshelf.Providers.ComicBook
{
    /// <summary>
    /// Handles metadata for comics which is saved as an XML document. This XML document is not part
    /// of the comic itself, but an external file.
    /// </summary>
    public class ExternalComicInfoProvider : IComicFileProvider
    {
        private readonly IFileSystem _fileSystem;

        private readonly ILogger<ExternalComicInfoProvider> _logger;

        private const string ComicRackMetaFile = "ComicInfo.xml";

        private ComicInfoXmlUtilities _utilities = new ComicInfoXmlUtilities();

        public ExternalComicInfoProvider(IFileSystem fileSystem, ILogger<ExternalComicInfoProvider> logger)
        {
            this._logger = logger;
            this._fileSystem = fileSystem;
        }

        public async Task<MetadataResult<Book>> ReadMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var comicInfoXml = await this.LoadXml(info, directoryService, cancellationToken);

            if (comicInfoXml is null)
            {
                this._logger.LogInformation("Could not load ComicInfo metadata for {0} from XML file", info.Path);
                return new MetadataResult<Book> { HasMetadata = false };
            }

            var book = this._utilities.ReadComicBookMetadata(comicInfoXml);

            //If we found no metadata about the book itself, abort mission
            if (book is null)
            {
                return new MetadataResult<Book> { HasMetadata = false };
            }

            var metadataResult = new MetadataResult<Book> { Item = book, HasMetadata = true };

            //We have found metadata like the title
            //let's search for the people like the author and save the found metadata
            this._utilities.ReadPeopleMetadata(comicInfoXml, metadataResult);

            this._utilities.ReadCultureInfoInto(comicInfoXml, "ComicInfo/LanguageISO", (cultureInfo) => metadataResult.ResultLanguage = cultureInfo.ThreeLetterISOLanguageName);

            return metadataResult;
        }

        public bool HasItemChanged(BaseItem item, IDirectoryService directoryService)
        {
            var file = this.GetXmlFilePath(item.Path);
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
                var reader = XmlReader.Create(path, new XmlReaderSettings { Async = true });
                var comicInfoXml = XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
                // Read data from XML
                return await comicInfoXml;
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, "Could not load external xml from {0}. This could mean there is no separate ComicInfo metadata file for this comic. Maybe the metadata is bundled within the comic", path);
                return null;
            }
        }

        private FileSystemMetadata GetXmlFilePath(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            var directoryInfo = fileInfo.IsDirectory ? fileInfo : _fileSystem.GetDirectoryInfo(Path.GetDirectoryName(path));

            var directoryPath = directoryInfo.FullName;

            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(path) + ".xml");

            var file = _fileSystem.GetFileInfo(specificFile);

            return file.Exists ? file : _fileSystem.GetFileInfo(Path.Combine(directoryPath, ComicRackMetaFile));
        }
    }
}
