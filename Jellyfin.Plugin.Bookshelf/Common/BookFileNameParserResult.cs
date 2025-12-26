using System;

namespace Jellyfin.Plugin.Bookshelf.Common;

/// <summary>
/// Data object used to pass result of the book name parsing.
/// </summary>
public struct BookFileNameParserResult : IEquatable<BookFileNameParserResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BookFileNameParserResult"/> struct.
    /// </summary>
    public BookFileNameParserResult()
    {
        Name = null;
        SeriesName = null;
        Index = null;
        Year = null;
    }

    /// <summary>
    /// Gets or sets the name of the book.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the series name.
    /// </summary>
    public string? SeriesName { get; set; }

    /// <summary>
    /// Gets or sets the book index.
    /// </summary>
    public int? Index { get; set; }

    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Compare two <see cref="BookFileNameParserResult"/> objects.
    /// </summary>
    /// <param name="left">Left object.</param>
    /// <param name="right">Right object.</param>
    /// <returns>True if the objects are equal.</returns>
    public static bool operator ==(BookFileNameParserResult left, BookFileNameParserResult right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compare two <see cref="BookFileNameParserResult"/> objects.
    /// </summary>
    /// <param name="left">Left object.</param>
    /// <param name="right">Right object.</param>
    /// <returns>True if the objects are not equal.</returns>
    public static bool operator !=(BookFileNameParserResult left, BookFileNameParserResult right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not BookFileNameParserResult)
        {
            return false;
        }

        return Equals((BookFileNameParserResult)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, SeriesName, Index, Year);
    }

    /// <inheritdoc />
    public bool Equals(BookFileNameParserResult other)
    {
        return Name == other.Name
            && SeriesName == other.SeriesName
            && Index == other.Index
            && Year == other.Year;
    }
}
