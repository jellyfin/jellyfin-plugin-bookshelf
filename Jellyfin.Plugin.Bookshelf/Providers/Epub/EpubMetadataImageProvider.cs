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

namespace Jellyfin.Plugin.Bookshelf.Providers.Epub
{
    /// <summary>
    /// Epub metadata image provider.
    /// </summary>
    public class EpubMetadataImageProvider : IDynamicImageProvider
    {
        private const string DcNamespace = @"http://purl.org/dc/elements/1.1/";
        private const string OpfNamespace = @"http://www.idpf.org/2007/opf";

        /// <inheritdoc />
        public string Name => "Epub Metadata";

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Book;
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc />
        public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            if (string.Equals(Path.GetExtension(item.Path), ".epub", StringComparison.OrdinalIgnoreCase))
            {
                return GetFromZip(item);
            }

            return Task.FromResult(new DynamicImageResponse { HasImage = false });
        }

        private bool IsValidImage(string? mimeType)
        {
            return !string.IsNullOrEmpty(mimeType)
                   && !string.IsNullOrWhiteSpace(MimeTypes.ToExtension(mimeType));
        }

        private EpubCover? ReadManifestItem(XmlNode manifestNode, string opfRootDirectory)
        {
            var href = manifestNode.Attributes?["href"]?.Value;
            var mediaType = manifestNode.Attributes?["media-type"]?.Value;

            if (string.IsNullOrEmpty(href)
                || string.IsNullOrEmpty(mediaType)
                || !IsValidImage(mediaType))
            {
                return null;
            }

            var coverPath = Path.Combine(opfRootDirectory, href);
            return new EpubCover(mediaType, coverPath);
        }

        private EpubCover? ReadCoverPath(XmlDocument opf, string opfRootDirectory)
        {
            var namespaceManager = new XmlNamespaceManager(opf.NameTable);
            namespaceManager.AddNamespace("dc", DcNamespace);
            namespaceManager.AddNamespace("opf", OpfNamespace);

            var coverImagePropertyNode = opf.SelectSingleNode("//opf:item[@properties='cover-image']", namespaceManager);
            if (coverImagePropertyNode is not null)
            {
                var coverImageProperty = ReadManifestItem(coverImagePropertyNode, opfRootDirectory);
                if (coverImageProperty != null)
                {
                    return coverImageProperty;
                }
            }

            var coverIdNode = opf.SelectSingleNode("//opf:item[@id='cover']", namespaceManager);
            if (coverIdNode is not null)
            {
                var coverId = ReadManifestItem(coverIdNode, opfRootDirectory);
                if (coverId != null)
                {
                    return coverId;
                }
            }

            var coverImageIdNode = opf.SelectSingleNode("//opf:item[@id='cover-image']", namespaceManager);
            if (coverImageIdNode is not null)
            {
                var coverImageId = ReadManifestItem(coverImageIdNode, opfRootDirectory);
                if (coverImageId != null)
                {
                    return coverImageId;
                }
            }

            var metaCoverImage = opf.SelectSingleNode("//opf:meta[@name='cover']", namespaceManager);
            var content = metaCoverImage?.Attributes?["content"]?.Value;
            if (string.IsNullOrEmpty(content) || metaCoverImage is null)
            {
                return null;
            }

            var coverPath = Path.Combine("Images", content);
            var coverFileManifest = opf.SelectSingleNode($"//opf:item[@href='{coverPath}']", namespaceManager);
            var mediaType = coverFileManifest?.Attributes?["media-type"]?.Value;
            if (coverFileManifest?.Attributes is not null
                && !string.IsNullOrEmpty(mediaType) && IsValidImage(mediaType))
            {
                return new EpubCover(mediaType, Path.Combine(opfRootDirectory, coverPath));
            }

            var coverFileIdManifest = opf.SelectSingleNode($"//opf:item[@id='{content}']", namespaceManager);
            if (coverFileIdManifest is not null)
            {
                return ReadManifestItem(coverFileIdManifest, opfRootDirectory);
            }

            return null;
        }

        private async Task<DynamicImageResponse> LoadCover(ZipArchive epub, XmlDocument opf, string opfRootDirectory)
        {
            var coverRef = ReadCoverPath(opf, opfRootDirectory);
            if (coverRef == null)
            {
                return new DynamicImageResponse { HasImage = false };
            }

            var cover = coverRef.Value;

            var coverFile = epub.GetEntry(cover.Path);
            if (coverFile == null)
            {
                return new DynamicImageResponse { HasImage = false };
            }

            var memoryStream = new MemoryStream();
            using (var coverStream = coverFile.Open())
            {
                await coverStream.CopyToAsync(memoryStream)
                    .ConfigureAwait(false);
            }

            memoryStream.Position = 0;

            var response = new DynamicImageResponse
            {
                HasImage = true,
                Stream = memoryStream
            };
            response.SetFormatFromMimeType(cover.MimeType);

            return response;
        }

        private Task<DynamicImageResponse> GetFromZip(BaseItem item)
        {
            using var epub = ZipFile.OpenRead(item.Path);

            var opfFilePath = EpubUtils.ReadContentFilePath(epub);
            if (opfFilePath == null)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            var opfRootDirectory = Path.GetDirectoryName(opfFilePath);
            if (string.IsNullOrEmpty(opfRootDirectory))
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            var opfFile = epub.GetEntry(opfFilePath);
            if (opfFile == null)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            using var opfStream = opfFile.Open();

            var opfDocument = new XmlDocument();
            opfDocument.Load(opfStream);

            return LoadCover(epub, opfDocument, opfRootDirectory);
        }

        private readonly struct EpubCover
        {
            public EpubCover(string coverMimeType, string coverPath)
            {
                (MimeType, Path) = (coverMimeType, coverPath);
            }

            public string MimeType { get; }

            public string Path { get; }
        }
    }
}
