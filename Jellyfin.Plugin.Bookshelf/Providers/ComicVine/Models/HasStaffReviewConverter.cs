using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models
{
    /// <summary>
    /// Custom JSON converter for the HasStaffReview property.
    /// This property can be either a boolean (false) or an object containing review details.
    /// </summary>
    internal class HasStaffReviewConverter : JsonConverter<StaffReview?>
    {
        /// <inheritdoc/>
        public override StaffReview? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // If it's a boolean, return null (no staff review)
            // Should only be false in this case
            if (reader.TokenType == JsonTokenType.False)
            {
                return null;
            }

            // If it's an object (review details), deserialize it
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                return JsonSerializer.Deserialize<StaffReview>(ref reader, options);
            }

            throw new JsonException($"Unexpected token type for HasStaffReview: {reader.TokenType}");
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, StaffReview? value, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Serialization is not implemented.");
        }
    }
}
