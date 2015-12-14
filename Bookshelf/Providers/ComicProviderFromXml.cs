using MBBookshelf.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CommonIO;

namespace MBBookshelf.Providers
{
    /// <summary>
    /// http://wiki.mobileread.com/wiki/CBR/CBZ#Metadata
    /// </summary>
    class ComicProviderFromXml : ILocalMetadataProvider<Book>, IHasChangeMonitor
    {
        private const string ComicRackMetaFile = "ComicInfo.xml";

        private readonly IFileSystem _fileSystem;

        public ComicProviderFromXml(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Reads the XML data.
        /// </summary>
        /// <param name="bookResult">The book result.</param>
        /// <param name="metaFile">The meta file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void ReadXmlData(MetadataResult<Book> bookResult, string metaFile, CancellationToken cancellationToken)
        {
            var book = bookResult.Item;

            cancellationToken.ThrowIfCancellationRequested();

            var doc = new XmlDocument();
            doc.Load(metaFile);

            var name = doc.SafeGetString("ComicInfo/Title");

            if (!string.IsNullOrEmpty(name))
                book.Name = name;

            var overview = doc.SafeGetString("ComicInfo/Summary");

            if (!string.IsNullOrEmpty(overview))
                book.Overview = overview;

            var publisher = doc.SafeGetString("ComicInfo/Publisher");

            if (!string.IsNullOrEmpty(publisher))
            {
                if (!book.Studios.Contains(publisher))
                    book.Studios.Add(publisher);
            }

            var author = doc.SafeGetString("ComicInfo/Writer");

            if (!string.IsNullOrEmpty(author))
            {
                var person = new PersonInfo { Name = author, Type = "Author" };

                bookResult.People.Add(person);
            }

        }

        private FileSystemMetadata GetXmlFile(string path, bool isInMixedFolder)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            var directoryInfo = fileInfo.IsDirectory ? fileInfo : _fileSystem.GetDirectoryInfo(Path.GetDirectoryName(path));

            var directoryPath = directoryInfo.FullName;

            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(path) + ".xml");

            var file = _fileSystem.GetFileInfo(specificFile);

            return file.Exists ? file : _fileSystem.GetFileInfo(Path.Combine(directoryPath, ComicRackMetaFile));
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            var file = GetXmlFile(item.Path, item.IsInMixedFolder);

            return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > date;
        }

        public Task<MetadataResult<Book>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = GetXmlFile(info.Path, info.IsInMixedFolder).FullName;

            var result = new MetadataResult<Book>();

            try
            {
                var item = new Book();
                result.HasMetadata = true;
                result.Item = item;
                ReadXmlData(result, path, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                result.HasMetadata = false;
            }

            return Task.FromResult(result);
        }

        public string Name
        {
            get { return "Comic Vine Xml"; }
        }

        public bool HasLocalMetadata(IHasMetadata item)
        {
            return GetXmlFile(item.Path, item.IsInMixedFolder).Exists;
        }
    }
}
