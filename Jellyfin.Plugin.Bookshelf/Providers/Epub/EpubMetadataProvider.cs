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
    public class EpubMetadataProvider : ILocalMetadataProvider<Book>
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public EpubMetadataProvider(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public string Name => "Epub Metadata";

        private FileSystemMetadata GetEpubFile(string path)
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

        private void ReadEpubAsZip(MetadataResult<Book> result, string path, CancellationToken cancellationToken)
        {
            using var epub = ZipFile.OpenRead(path);

            var opfFilePath = EpubUtils.ReadContentFilePath(epub);
            if (opfFilePath == null)
            {
                return;
            }

            var opf = epub.GetEntry(opfFilePath);
            if (opf == null)
            {
                return;
            }

            using var opfStream = opf.Open();

            var opfDocument = new XmlDocument();
            opfDocument.Load(opfStream);

            OpfReader.ReadOpfData(result, opfDocument, cancellationToken, _logger);
        }

        public Task<MetadataResult<Book>> GetMetadata(
            ItemInfo info,
            IDirectoryService directoryService,
            CancellationToken cancellationToken)
        {
            var path = GetEpubFile(info.Path)?.FullName;
            var result = new MetadataResult<Book>();

            if (path == null)
            {
                result.HasMetadata = false;
            }
            else
            {
                var item = new Book();
                result.HasMetadata = true;
                result.Item = item;
                ReadEpubAsZip(result, path, cancellationToken);
            }

            return Task.FromResult(result);
        }
    }
}