using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers
{
    /// <summary>
    /// OPF book provider.
    /// </summary>
    public class BookProviderFromOpf : ILocalMetadataProvider<Book>, IHasItemChangeMonitor
    {
        private const string StandardOpfFile = "content.opf";
        private const string CalibreOpfFile = "metadata.opf";

        private readonly IFileSystem _fileSystem;

        private readonly ILogger<BookProviderFromOpf> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BookProviderFromOpf"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{BookProviderFromOpf}"/> interface.</param>
        public BookProviderFromOpf(IFileSystem fileSystem, ILogger<BookProviderFromOpf> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "Open Packaging Format";

        /// <inheritdoc />
        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            var file = GetXmlFile(item.Path);
            return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
        }

        /// <inheritdoc />
        public Task<MetadataResult<Book>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = GetXmlFile(info.Path).FullName;

            try
            {
                var result = ReadOpfData(path, cancellationToken);

                if (result is null)
                {
                    return Task.FromResult(new MetadataResult<Book> { HasMetadata = false });
                }
                else
                {
                    return Task.FromResult(result);
                }
            }
            catch (FileNotFoundException)
            {
                return Task.FromResult(new MetadataResult<Book> { HasMetadata = false });
            }
        }

        private FileSystemMetadata GetXmlFile(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            var directoryInfo = fileInfo.IsDirectory ? fileInfo : _fileSystem.GetDirectoryInfo(Path.GetDirectoryName(path)!);

            var directoryPath = directoryInfo.FullName;

            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(path) + ".opf");

            var file = _fileSystem.GetFileInfo(specificFile);

            if (file.Exists)
            {
                return file;
            }

            file = _fileSystem.GetFileInfo(Path.Combine(directoryPath, StandardOpfFile));

            return file.Exists ? file : _fileSystem.GetFileInfo(Path.Combine(directoryPath, CalibreOpfFile));
        }

        private MetadataResult<Book> ReadOpfData(string metaFile, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var doc = new XmlDocument();
            doc.Load(metaFile);

            var utilities = new OpfReader<BookProviderFromOpf>(doc, _logger);
            return utilities.ReadOpfData(cancellationToken);
        }
    }
}
