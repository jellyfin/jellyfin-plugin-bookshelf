using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.Epub
{
    /// <summary>
    /// Epub metadata provider.
    /// </summary>
    public class EpubMetadataProvider : ILocalMetadataProvider<Book>
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<EpubMetadataProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpubMetadataProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{EpubMetadataProvider}"/> interface.</param>
        public EpubMetadataProvider(IFileSystem fileSystem, ILogger<EpubMetadataProvider> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "Epub Metadata";

        /// <inheritdoc />
        public Task<MetadataResult<Book>> GetMetadata(
            ItemInfo info,
            IDirectoryService directoryService,
            CancellationToken cancellationToken)
        {
            var path = GetEpubFile(info.Path)?.FullName;

            if (path is null)
            {
                return Task.FromResult(new MetadataResult<Book> { HasMetadata = false });
            }

            var result = ReadEpubAsZip(path, cancellationToken);

            if (result is null)
            {
                return Task.FromResult(new MetadataResult<Book> { HasMetadata = false });
            }
            else
            {
                return Task.FromResult(result);
            }
        }

        private FileSystemMetadata? GetEpubFile(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.IsDirectory)
            {
                return null;
            }

            if (!string.Equals(Path.GetExtension(fileInfo.FullName), ".epub", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fileInfo;
        }

        private MetadataResult<Book>? ReadEpubAsZip(string path, CancellationToken cancellationToken)
        {
            using var epub = ZipFile.OpenRead(path);

            var opfFilePath = EpubUtils.ReadContentFilePath(epub);
            if (opfFilePath == null)
            {
                return null;
            }

            var opf = epub.GetEntry(opfFilePath);
            if (opf == null)
            {
                return null;
            }

            using var opfStream = opf.Open();

            var opfDocument = new XmlDocument();
            opfDocument.Load(opfStream);

            var utilities = new OpfReader<EpubMetadataProvider>(opfDocument, _logger);
            return utilities.ReadOpfData(cancellationToken);
        }
    }
}
