using System.Text.Json;
using System.Text.Json.Serialization;
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
            Assert.Null(result.StaffReview);
        }

        [Fact]
        public void Deserialize_WithBooleanTrue_ThrowsException()
        {
            var json = @"{""has_staff_review"": true}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TestModel>(json, _jsonOptions));
        }

        [Fact]
        public void Deserialize_WithNull_ReturnsNull()
        {
            var json = @"{""has_staff_review"": null}";

            var result = JsonSerializer.Deserialize<TestModel>(json, _jsonOptions);

            Assert.NotNull(result);
            Assert.Null(result.StaffReview);
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
            Assert.NotNull(result.StaffReview);
            Assert.Equal(3467, result.StaffReview.Id);
            Assert.Equal("Aliens: Fire and Stone #1 Review", result.StaffReview.Name);
            Assert.Equal("https://comicvine.gamespot.com/api/review/1900-3467/", result.StaffReview.ApiDetailUrl);
            Assert.Equal("https://comicvine.gamespot.com/reviews/aliens-fire-and-stone-1/1900-3467/", result.StaffReview.SiteDetailUrl);
        }

        [Fact]
        public void Serialize_WithNull_WritesNull()
        {
            var model = new TestModel { StaffReview = null };

            var json = JsonSerializer.Serialize(model, _jsonOptions);

            Assert.Contains("\"has_staff_review\":null", json);
        }

        [Fact]
        public void Serialize_WithStaffReview_WritesObject()
        {
            var model = new TestModel
            {
                StaffReview = new StaffReview
                {
                    Id = 3467,
                    Name = "Test Review",
                    ApiDetailUrl = "https://test.com/api",
                    SiteDetailUrl = "https://test.com/review"
                }
            };

            var json = JsonSerializer.Serialize(model, _jsonOptions);

            Assert.Contains("\"has_staff_review\":", json);
            Assert.Contains("\"id\":3467", json);
            Assert.Contains("\"name\":\"Test Review\"", json);
        }

        private class TestModel
        {
            [JsonPropertyName("has_staff_review")]
            [JsonConverter(typeof(HasStaffReviewConverter))]
            public StaffReview? StaffReview { get; set; }
        }
    }
}
