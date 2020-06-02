using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.Epub
{
    public class EpubMetadataImageProvider : IDynamicImageProvider
    {
        private const string DcNamespace = @"http://purl.org/dc/elements/1.1/";
        private const string OpfNamespace = @"http://www.idpf.org/2007/opf";

        private readonly ILogger<EpubMetadataImageProvider> _logger;

        public EpubMetadataImageProvider(ILogger<EpubMetadataImageProvider> logger)
        {
            _logger = logger;
        }

        public string Name => "Epub Metadata";

        private readonly struct EpubCover
        {
            public string MimeType { get; }
            public string Path { get; }

            public EpubCover(string coverMimeType, string coverPath) =>
                (this.MimeType, this.Path) = (coverMimeType, coverPath);
        }

        public bool Supports(BaseItem item)
        {
            return item is Book;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType> {ImageType.Primary};
        }

        private bool IsValidImage(string mimeType)
        {
            return !string.IsNullOrWhiteSpace(MimeTypes.ToExtension(mimeType));
        }

        private EpubCover? ReadManifestItem(XmlNode manifestNode, string opfRootDirectory)
        {
            if (manifestNode?.Attributes?["href"]?.Value != null &&
                manifestNode.Attributes?["media-type"]?.Value != null &&
                IsValidImage(manifestNode.Attributes["media-type"].Value))
            {
                var coverMimeType = manifestNode.Attributes["media-type"].Value;
                var coverPath = Path.Combine(opfRootDirectory, manifestNode.Attributes["href"].Value);
                return new EpubCover(coverMimeType, coverPath);
            }
            else
            {
                return null;
            }
        }

        private EpubCover? ReadCoverPath(XmlDocument opf, string opfRootDirectory)
        {
            var namespaceManager = new XmlNamespaceManager(opf.NameTable);
            namespaceManager.AddNamespace("dc", DcNamespace);
            namespaceManager.AddNamespace("opf", OpfNamespace);

            var coverImagePropertyNode =
                opf.SelectSingleNode("//opf:item[@properties='cover-image']", namespaceManager);
            var coverImageProperty = ReadManifestItem(coverImagePropertyNode, opfRootDirectory);
            if (coverImageProperty != null)
            {
                return coverImageProperty;
            }

            var coverIdNode =
                opf.SelectSingleNode("//opf:item[@id='cover']", namespaceManager);
            var coverId = ReadManifestItem(coverIdNode, opfRootDirectory);
            if (coverId != null)
            {
                return coverId;
            }

            var coverImageIdNode =
                opf.SelectSingleNode("//opf:item[@id='cover-image']", namespaceManager);
            var coverImageId = ReadManifestItem(coverImageIdNode, opfRootDirectory);
            if (coverImageId != null)
            {
                return coverImageId;
            }

            var metaCoverImage = opf.SelectSingleNode("//opf:meta[@name='cover']", namespaceManager);
            if (metaCoverImage?.Attributes?["content"]?.Value != null)
            {
                var metaContent = metaCoverImage.Attributes["content"].Value;
                var coverPath = Path.Combine("Images", metaContent);
                var coverFileManifest = opf.SelectSingleNode($"//opf:item[@href='{coverPath}']", namespaceManager);
                if (coverFileManifest?.Attributes?["media-type"]?.Value != null &&
                    IsValidImage(coverFileManifest.Attributes["media-type"].Value))
                {
                    var coverMimeType = coverFileManifest.Attributes["media-type"].Value;
                    return new EpubCover(coverMimeType, Path.Combine(opfRootDirectory, coverPath));
                }

                var coverFileIdManifest = opf.SelectSingleNode($"//opf:item[@id='{metaContent}']", namespaceManager);
                return ReadManifestItem(coverFileIdManifest, opfRootDirectory);
            }

            return null;
        }

        private Task<DynamicImageResponse> LoadCover(ZipArchive epub, XmlDocument opf, string opfRootDirectory)
        {
            var coverRef = ReadCoverPath(opf, opfRootDirectory);
            if (coverRef == null)
            {
                return Task.FromResult(new DynamicImageResponse {HasImage = false});
            }

            var cover = coverRef.Value;

            var coverFile = epub.GetEntry(cover.Path);
            if (coverFile == null)
            {
                return Task.FromResult(new DynamicImageResponse {HasImage = false});
            }

            var memoryStream = new MemoryStream();
            using (var coverStream = coverFile.Open())
            {
                coverStream.CopyTo(memoryStream);
            }

            memoryStream.Position = 0;

            var response = new DynamicImageResponse
            {
                HasImage = true,
                Stream = memoryStream
            };
            response.SetFormatFromMimeType(cover.MimeType);

            return Task.FromResult(response);
        }

        private Task<DynamicImageResponse> GetFromZip(BaseItem item)
        {
            using var epub = ZipFile.OpenRead(item.Path);

            var opfFilePath = EpubUtils.ReadContentFilePath(epub);
            if (opfFilePath == null)
            {
                return Task.FromResult(new DynamicImageResponse {HasImage = false});
            }

            var opfRootDirectory = Path.GetDirectoryName(opfFilePath);

            var opfFile = epub.GetEntry(opfFilePath);
            if (opfFile == null)
            {
                return Task.FromResult(new DynamicImageResponse {HasImage = false});
            }

            using var opfStream = opfFile.Open();

            var opfDocument = new XmlDocument();
            opfDocument.Load(opfStream);

            return LoadCover(epub, opfDocument, opfRootDirectory);
        }

        public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            if (string.Equals(Path.GetExtension(item.Path), ".epub", StringComparison.OrdinalIgnoreCase))
            {
                return GetFromZip(item);
            }
            else
            {
                return Task.FromResult(new DynamicImageResponse {HasImage = false});
            }
        }
    }
}