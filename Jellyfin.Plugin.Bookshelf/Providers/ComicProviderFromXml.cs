using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Plugin.Bookshelf.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.Bookshelf.Providers
{
    /// <summary>
    ///     http://wiki.mobileread.com/wiki/CBR/CBZ#Metadata.
    /// </summary>
    public class ComicProviderFromXml : ILocalMetadataProvider<Book>, IHasItemChangeMonitor
    {
        private const string ComicRackMetaFile = "ComicInfo.xml";

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComicProviderFromXml"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        public ComicProviderFromXml(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string Name => "Comic Vine XML";

        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            var file = GetXmlFile(item.Path);
            return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
        }

        public Task<MetadataResult<Book>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = GetXmlFile(info.Path).FullName;

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

        /// <summary>
        ///     Reads the XML data.
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
            {
                book.Name = name;
            }

            var overview = doc.SafeGetString("ComicInfo/Summary");

            if (!string.IsNullOrEmpty(overview))
            {
                book.Overview = overview;
            }

            var publisher = doc.SafeGetString("ComicInfo/Publisher");

            if (!string.IsNullOrEmpty(publisher))
            {
                book.SetStudios(new[] { publisher });
            }

            var author = doc.SafeGetString("ComicInfo/Writer");

            if (!string.IsNullOrEmpty(author))
            {
                var person = new PersonInfo { Name = author, Type = "Author" };
                bookResult.People.Add(person);
            }
        }

        private FileSystemMetadata GetXmlFile(string path)
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