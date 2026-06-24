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
    /// <summary>
    /// Epub metadata image provider.
    /// </summary>
    public class EpubMetadataImageProvider : IDynamicImageProvider
    {
        private readonly ILogger<EpubMetadataImageProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpubMetadataImageProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{EpubMetadataImageProvider}"/> interface.</param>
        public EpubMetadataImageProvider(ILogger<EpubMetadataImageProvider> logger)
        {
            _logger = logger;
        }

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
                return GetFromZip(item, cancellationToken);
            }

            return Task.FromResult(new DynamicImageResponse { HasImage = false });
        }

        private async Task<DynamicImageResponse> LoadCover(ZipArchive epub, XmlDocument opf, string opfRootDirectory, CancellationToken cancellationToken)
        {
            var utilities = new OpfReader<EpubMetadataImageProvider>(opf, _logger);
            var coverRef = utilities.ReadCoverPath(opfRootDirectory);
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
            #pragma warning disable CA2007
            await using (var coverStream = await coverFile.OpenAsync(cancellationToken).ConfigureAwait(false))
            #pragma warning restore CA2007
            {
                await coverStream.CopyToAsync(memoryStream, cancellationToken)
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

        private async Task<DynamicImageResponse> GetFromZip(BaseItem item, CancellationToken cancellationToken)
        {
            #pragma warning disable CA2007
            await using var epub = await ZipFile.OpenReadAsync(item.Path, cancellationToken).ConfigureAwait(false);
            #pragma warning restore CA2007

            var opfFilePath = EpubUtils.ReadContentFilePath(epub);
            if (opfFilePath == null)
            {
                return new DynamicImageResponse { HasImage = false };
            }

            var opfRootDirectory = Path.GetDirectoryName(opfFilePath);
            if (opfRootDirectory == null)
            {
                return new DynamicImageResponse { HasImage = false };
            }

            var opfFile = epub.GetEntry(opfFilePath);
            if (opfFile == null)
            {
                return new DynamicImageResponse { HasImage = false };
            }

            #pragma warning disable CA2007
            await using var opfStream = await opfFile.OpenAsync(cancellationToken).ConfigureAwait(false);
            #pragma warning restore CA2007

            var opfDocument = new XmlDocument();
            opfDocument.Load(opfStream);

            return await LoadCover(epub, opfDocument, opfRootDirectory, cancellationToken).ConfigureAwait(false);
        }
    }
}
