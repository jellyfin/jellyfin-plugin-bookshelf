using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Bookshelf.Common
{
    /// <summary>
    /// Helper class to retrieve name, year, index and series name from a book name and parent.
    /// </summary>
    public static class BookFileNameParser
    {
        // convert these characters to whitespace for better matching
        // there are two dashes with different char codes
        private const string Spacers = "/,.:;\\(){}[]+-_=–*";

        private const string Remove = "\"'!`?";

        private const string NameMatchGroup = "name";
        private const string SeriesNameMatchGroup = "seriesName";
        private const string IndexMatchGroup = "index";
        private const string YearMatchGroup = "year";

        private static readonly Regex[] _nameMatches =
        {
            // seriesName (seriesYear) #index (of count) (year), with only seriesName and index required
            new Regex(@"^(?<seriesName>.+?)((\s\((?<seriesYear>[0-9]{4})\))?)\s#(?<index>[0-9]+)((\s\(of\s(?<count>[0-9]+)\))?)((\s\((?<year>[0-9]{4})\))?)$"),
            // name (seriesName, #index) (year), with year optional
            new Regex(@"^(?<name>.+?)\s\((?<seriesName>.+?),\s#(?<index>[0-9]+)\)((\s\((?<year>[0-9]{4})\))?)$"),
            // index - name (year), with year optional
            new Regex(@"^(?<index>[0-9]+)\s\-\s(?<name>.+?)((\s\((?<year>[0-9]{4})\))?)$"),
            // name (year)
            new Regex(@"(?<name>.*)\((?<year>[0-9]{4})\)"),
            // last resort matches the whole string as the name
            new Regex(@"(?<name>.*)")
        };

        private static readonly Dictionary<string, string> _replaceEndNumerals = new()
        {
            { " i", " 1" },
            { " ii", " 2" },
            { " iii", " 3" },
            { " iv", " 4" },
            { " v", " 5" },
            { " vi", " 6" },
            { " vii", " 7" },
            { " viii", " 8" },
            { " ix", " 9" },
            { " x", " 10" }
        };

        /// <summary>
        /// Parse the book name and parent folder name to retrieve the book name, series name, index and year.
        /// </summary>
        /// <param name="name">Book file name.</param>
        /// <param name="seriesName">Book series name.</param>
        /// <returns>The parsing result.</returns>
        public static BookFileNameParserResult Parse(string name, string seriesName)
        {
            BookFileNameParserResult result = default;

            foreach (var regex in _nameMatches)
            {
                var match = regex.Match(name);

                if (!match.Success)
                {
                    continue;
                }

                if (match.Groups.TryGetValue(NameMatchGroup, out Group? nameGroup) && nameGroup.Success)
                {
                    result.Name = nameGroup.Value.Trim();
                }

                if (match.Groups.TryGetValue(SeriesNameMatchGroup, out Group? seriesGroup) && seriesGroup.Success)
                {
                    result.SeriesName = seriesGroup.Value.Trim();
                }

                if (match.Groups.TryGetValue(IndexMatchGroup, out Group? indexGroup)
                    && indexGroup.Success
                    && int.TryParse(indexGroup.Value, out var index))
                {
                     result.Index = index;
                }

                if (match.Groups.TryGetValue(YearMatchGroup, out Group? yearGroup)
                    && yearGroup.Success
                    && int.TryParse(yearGroup.Value, out var year))
                {
                    result.Year = year;
                }

                break;
            }

            // If the book is in a folder, the folder's name will be set as the series name
            // If it's not in a folder, the series name will be set to the name of the collection
            // So if we couldn't find the series name in the book name, use the folder name instead
            if (string.IsNullOrWhiteSpace(result.SeriesName) && !string.Equals(seriesName, "books", StringComparison.OrdinalIgnoreCase))
            {
                result.SeriesName = seriesName;
            }

            return result;
        }

        /// <summary>
        /// Format a string to make it easier to compare.
        /// </summary>
        /// <param name="value">Value to format.</param>
        /// <param name="replaceEndNumerals">Whether end numerals should be replaced.</param>
        /// <returns>The formatted string.</returns>
        public static string GetComparableString(string value, bool replaceEndNumerals)
        {
            value = value.ToLower(CultureInfo.InvariantCulture);
            value = value.Normalize(NormalizationForm.FormC);

            if (replaceEndNumerals)
            {
                string? endNumerals = _replaceEndNumerals.Keys.FirstOrDefault(key => value.EndsWith(key, StringComparison.OrdinalIgnoreCase));

                if (endNumerals != null)
                {
                    var replacement = _replaceEndNumerals[endNumerals];

                    value = value.Remove(value.Length - endNumerals.Length, endNumerals.Length);
                    value += replacement;
                }
            }

            var sb = new StringBuilder();
            foreach (var c in value)
            {
                if (c >= 0x2B0 && c <= 0x0333)
                {
                    // skip char modifier and diacritics
                }
                else if (Remove.IndexOf(c, StringComparison.Ordinal) > -1)
                {
                    // skip chars we are removing
                }
                else if (Spacers.IndexOf(c, StringComparison.Ordinal) > -1)
                {
                    sb.Append(' ');
                }
                else if (c == '&')
                {
                    sb.Append(" and ");
                }
                else
                {
                    sb.Append(c);
                }
            }

            value = sb.ToString();
            value = value.Replace("the", " ", StringComparison.OrdinalIgnoreCase);
            value = value.Replace(" - ", ": ", StringComparison.Ordinal);

            var regex = new Regex(@"\s+");
            value = regex.Replace(value, " ");

            return value.Trim();
        }

        /// <summary>
        /// Gets the extracted metadata from the item's properties.
        /// </summary>
        /// <param name="item">The info item.</param>
        /// <returns>The extracted metadata.</returns>
        public static BookInfo GetBookMetadata(BookInfo item)
        {
            var nameParserResult = Parse(item.Name, item.SeriesName);

            return new BookInfo()
            {
                Name = nameParserResult.Name ?? string.Empty,
                SeriesName = nameParserResult.SeriesName ?? string.Empty,
                IndexNumber = nameParserResult.Index,
                Year = nameParserResult.Year,
            };
        }
    }
}
