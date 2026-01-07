using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using PDFtoImage;
using SkiaSharp;

namespace Jellyfin.Plugin.Pdfcover.Providers
{
    /// <summary>
    /// PDF cover provider.
    /// </summary>
    public class PDFCoverProvider : IDynamicImageProvider
    {
        private readonly string _pdfExtension = ".pdf";
        private readonly ILogger<PDFCoverProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PDFCoverProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{PDFCoverProvider}"/> interface.</param>
        public PDFCoverProvider(ILogger<PDFCoverProvider> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "PDF Cover Generator";

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
        public async Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            if (!string.Equals(Path.GetExtension(item.Path), _pdfExtension, StringComparison.OrdinalIgnoreCase))
            {
                return new DynamicImageResponse { HasImage = false };
            }

            _logger.LogInformation("Attempting to create PDF cover for {Path}", item.Path);
            try
            {
                // Run the synchronous PDF conversion in a background thread
                return await Task.Run(
                    () =>
                    {
                        using var fileStream = File.OpenRead(item.Path);
                        var ms = new MemoryStream();

#pragma warning disable CA1416
                        using var bitmap = Conversion.ToImage(fileStream, 0);
#pragma warning restore CA1416

                        if (bitmap == null)
                        {
                            return new DynamicImageResponse { HasImage = false };
                        }

                        using (var image = SKImage.FromBitmap(bitmap))
                        using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 90))
                        {
                            data.SaveTo(ms);
                        }

                        ms.Position = 0;

                        _logger.LogInformation("Successfully created PDF cover for {Path}", item.Path);
                        return new DynamicImageResponse
                        {
                            HasImage = true,
                            Stream = ms,
                            Format = ImageFormat.Jpg,
                        };
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Log and return nothing
                _logger.LogError(e, "Failed to load cover from {Path}", item.Path);
                return new DynamicImageResponse { HasImage = false };
            }
        }
    }
}
