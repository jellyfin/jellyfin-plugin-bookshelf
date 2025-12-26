using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models;

/// <summary>
/// Details of a person.
/// </summary>
public class PersonDetails : PersonCredit
{
    /// <summary>
    /// Gets the list of aliases the person is known by. A \n (newline) seperates each alias.
    /// </summary>
    public string? Aliases { get; init; }

    /// <summary>
    /// Gets a date, if one exists, that the person was born on. Not an origin date.
    /// </summary>
    public string? Birth { get; init; }

    /// <summary>
    /// Gets a date, if one exists, that the person was born on. Not an origin date.
    /// </summary>
    [JsonIgnore]
    public DateTime? BirthDate => ParseAsUTC(Birth);

    /// <summary>
    /// Gets the country the person resides in.
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Gets the date this person died on.
    /// </summary>
    public string? Death { get; init; }

    /// <summary>
    /// Gets the date this person died on.
    /// </summary>
    [JsonIgnore]
    public DateTime? DeathDate => ParseAsUTC(Death);

    /// <summary>
    /// Gets a brief summary of the person.
    /// </summary>
    public string? Deck { get; init; }

    /// <summary>
    /// Gets the description of the person.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the email of this person.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets the gender of the person. Available options are: Male (1), Female (2), Other (0).
    /// </summary>
    public int? Gender { get; init; }

    /// <summary>
    /// Gets the city or town the person resides in.
    /// </summary>
    public string? Hometown { get; init; }

    /// <summary>
    /// Gets the main image of the person.
    /// </summary>
    public ImageList? Image { get; init; }

    /// <summary>
    /// Gets the URL to the person website.
    /// </summary>
    public string? Website { get; init; }

    private static DateTime? ParseAsUTC(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime result))
        {
            return result;
        }

        return null;
    }
}
