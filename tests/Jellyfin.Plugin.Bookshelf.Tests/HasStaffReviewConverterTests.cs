using System.Text.Json;
using Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models;

namespace Jellyfin.Plugin.Bookshelf.Tests
{
    public class HasStaffReviewConverterTests
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public HasStaffReviewConverterTests()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        [Fact]
        public void Deserialize_WithBooleanFalse_ReturnsNull()
        {
            var json = @"{""has_staff_review"": false}";

            var result = JsonSerializer.Deserialize<TestModel>(json, _jsonOptions);

            Assert.NotNull(result);
            Assert.Null(result.HasStaffReview);
        }

        [Fact]
        public void Deserialize_WithBooleanTrue_ThrowsException()
        {
            var json = @"{""has_staff_review"": true}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TestModel>(json, _jsonOptions));
        }

        [Fact]
        public void Deserialize_WithStaffReviewObject_ReturnsStaffReview()
        {
            var json = @"{
                ""has_staff_review"": {
                    ""api_detail_url"": ""https://comicvine.gamespot.com/api/review/1900-3467/"",
                    ""id"": 3467,
                    ""name"": ""Aliens: Fire and Stone #1 Review"",
                    ""site_detail_url"": ""https://comicvine.gamespot.com/reviews/aliens-fire-and-stone-1/1900-3467/""
                }
            }";

            var result = JsonSerializer.Deserialize<TestModel>(json, _jsonOptions);

            Assert.NotNull(result);
            Assert.NotNull(result.HasStaffReview);
            Assert.Equal(3467, result.HasStaffReview.Id);
            Assert.Equal("Aliens: Fire and Stone #1 Review", result.HasStaffReview.Name);
            Assert.Equal("https://comicvine.gamespot.com/api/review/1900-3467/", result.HasStaffReview.ApiDetailUrl);
            Assert.Equal("https://comicvine.gamespot.com/reviews/aliens-fire-and-stone-1/1900-3467/", result.HasStaffReview.SiteDetailUrl);
        }

        private class TestModel
        {
            [System.Text.Json.Serialization.JsonConverter(typeof(HasStaffReviewConverter))]
            public StaffReview? HasStaffReview { get; set; }
        }
    }
}
