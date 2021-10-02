using System;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

#nullable enable
namespace Jellyfin.Plugin.Bookshelf.Providers.ComicBook
{
    /// <summary>
    /// Handles metadata for comics which is saved as an XML document inside of the comic itself.
    /// </summary>
    public class InternalComicInfoProvider : IComicFileProvider
    {
        private readonly IFileSystem _fileSystem;

        private readonly ILogger<InternalComicInfoProvider> _logger;

        private ComicInfoXmlUtilities _utilities = new ComicInfoXmlUtilities();

        public InternalComicInfoProvider(IFileSystem fileSystem, ILogger<InternalComicInfoProvider> logger)
        {
            this._logger = logger;
            this._fileSystem = fileSystem;
        }

        public async Task<MetadataResult<Book>> ReadMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var comicInfoXml = await this.LoadXml(info, directoryService, cancellationToken);

            if (comicInfoXml is null)
            {
                this._logger.LogInformation($"Could not load ComicInfo metadata for ${info.Path} from XML file. No internal XML in comic archive.");
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
            var file = this.GetComicBookFile(item.Path);

            if (file is null)
            {
                return false;
            }

            return file.Exists && this._fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
        }

        protected async Task<XDocument?> LoadXml(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = this.GetComicBookFile(info.Path)?.FullName;

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
                using var containerStream = container.Open();
                var comicInfoXml = XDocument.LoadAsync(containerStream, LoadOptions.None, cancellationToken);

                // Read data from XML
                return await comicInfoXml;
            }
            catch (Exception e)
            {
                this._logger.LogError(e, $"Could not load internal xml from {path}", null);
                return null;
            }
        }

        private FileSystemMetadata? GetComicBookFile(string path)
        {
            var fileInfo = this._fileSystem.GetFileSystemInfo(path);

            if (fileInfo.IsDirectory)
            {
                return null;
            }

            // Only parse files that are known to have internal metadata
            if (fileInfo.Extension != ".cbz")
            {
                return null;
            }

            return fileInfo;
        }
    }
}
