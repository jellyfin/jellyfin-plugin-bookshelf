using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Pdfcover.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.PDFCover.Tests
{
    public class PDFCoverProviderTests
    {
        private readonly PDFCoverProvider _provider;
        private readonly Mock<ILogger<PDFCoverProvider>> _loggerMock;

        public PDFCoverProviderTests()
        {
            _loggerMock = new Mock<ILogger<PDFCoverProvider>>();
            _provider = new PDFCoverProvider(_loggerMock.Object);
        }

        [Fact]
        public void Name_Returns_CorrectName()
        {
            Assert.Equal("PDF Cover Generator", _provider.Name);
        }

        [Fact]
        public void Supports_GivenBook_ReturnsTrue()
        {
            Assert.True(_provider.Supports(new Book()));
        }

        [Fact]
        public void Supports_GivenNonBook_ReturnsFalse()
        {
            // Use Folder as a representative non-book type
            Assert.False(_provider.Supports(new Folder()));
        }

        [Fact]
        public void GetSupportedImages_Returns_Primary()
        {
            var supportedImages = _provider.GetSupportedImages(new Book());
            Assert.Single(supportedImages);
            Assert.Equal(ImageType.Primary, supportedImages.First());
        }

        [Fact]
        public async Task GetImage_GivenNonPdfFile_ReturnsHasImageFalse()
        {
            var book = new Book { Path = "TestAssets/dummy.txt" };
            var result = await _provider.GetImage(book, ImageType.Primary, CancellationToken.None);
            Assert.False(result.HasImage);
        }

        [Fact]
        public async Task GetImage_GivenValidPdfFile_ReturnsImage()
        {
            var book = new Book { Path = "TestAssets/dummy.pdf" };
            var result = await _provider.GetImage(book, ImageType.Primary, CancellationToken.None);

            // Verify logs first to get more information on failure
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Attempting to create PDF cover for TestAssets/dummy.pdf")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Successfully created PDF cover for TestAssets/dummy.pdf")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            if (result.HasImage)
            {
                Assert.NotNull(result.Stream);
                Assert.True(result.Stream.Length > 0);
                Assert.Equal(ImageFormat.Jpg, result.Format);
                result.Stream.Dispose();
            }

            // Assert this last, so we can see logger failures first.
            Assert.True(result.HasImage);
        }

        [Fact]
        public async Task GetImage_GivenCorruptPdfFile_ReturnsHasImageFalse()
        {
            var book = new Book { Path = "TestAssets/corrupt.pdf" };
            var result = await _provider.GetImage(book, ImageType.Primary, CancellationToken.None);

            Assert.False(result.HasImage);
        }

        [Fact]
        public async Task GetImage_GivenNonExistentFile_ReturnsHasImageFalse()
        {
            var book = new Book { Path = "nonexistent.pdf" };
            var result = await _provider.GetImage(book, ImageType.Primary, CancellationToken.None);

            Assert.False(result.HasImage);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Failed to load cover from nonexistent.pdf")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
