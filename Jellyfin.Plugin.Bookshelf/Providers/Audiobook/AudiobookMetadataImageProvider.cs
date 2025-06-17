using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.Audiobook
{
    /// <summary>
    /// Audiobook metadata image provider for extracting cover art from audiobook files.
    /// </summary>
    public class AudiobookMetadataImageProvider : IDynamicImageProvider
    {
        private readonly ILogger<AudiobookMetadataImageProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudiobookMetadataImageProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{AudiobookMetadataImageProvider}"/> interface.</param>
        public AudiobookMetadataImageProvider(ILogger<AudiobookMetadataImageProvider> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "Audiobook Metadata";

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            string extension = Path.GetExtension(item.Path);
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            return item is AudioBook &&
                   !string.IsNullOrEmpty(item.Path) &&
                     AudiobookUtils.SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc />
        public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            if (type != ImageType.Primary)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            if (!AudiobookUtils.IsValidAudiobookFile(item.Path))
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            try
            {
                return ExtractCoverFromAudiobook(item.Path, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting cover from Audiobook file: {Path}", item.Path);
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }
        }

        private Task<DynamicImageResponse> ExtractCoverFromAudiobook(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                using var file = TagLib.File.Create(filePath);
                var tag = file.Tag;

                if (tag?.Pictures == null || tag.Pictures.Length == 0)
                {
                    return Task.FromResult(new DynamicImageResponse { HasImage = false });
                }

                // Get the first picture (usually the cover)
                var picture = tag.Pictures[0];

                if (picture.Data?.Data == null || picture.Data.Data.Length == 0)
                {
                    return Task.FromResult(new DynamicImageResponse { HasImage = false });
                }

                var memoryStream = new MemoryStream(picture.Data.Data);

                var response = new DynamicImageResponse
                {
                    HasImage = true,
                    Stream = memoryStream
                };

                // Set the format based on the MIME type
                if (!string.IsNullOrEmpty(picture.MimeType))
                {
                    response.SetFormatFromMimeType(picture.MimeType);
                }

                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract cover art from Audiobook file: {Path}", filePath);
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }
        }
    }
}
