using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicInfo
{
    /// <summary>
    /// Comic info xml utilities.
    /// </summary>
    public class ComicInfoXmlUtilities : IComicInfoXmlUtilities
    {
        /// <inheritdoc />
        public Book? ReadComicBookMetadata(XDocument xml)
        {
            var book = new Book();
            var hasFoundMetadata = false;

            hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/Title", title => book.Name = title);
            // this value is used internally only, as Jellyfin has no field to save it to
            var isManga = false;

            hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/Title", title => book.Name = title);
            hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/Manga", manga =>
            {
                if (manga.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                {
                    isManga = true;
                }
            });
            hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/AlternateSeries", title =>
            {
                if (isManga)
                {
                    // Software like ComicTagger (https://github.com/comictagger/comictagger) uses
                    // this field for the series name in the original language when tagging manga
                    book.OriginalTitle = title;
                }
                else
                {
                    // Based on the The Anansi Project, some US comics can be part of cross-over
                    // story arcs. This field is then used to specify an alternate series
                }
            });
            hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/Series", series => book.SeriesName = series);
            hasFoundMetadata |= ReadIntInto(xml, "ComicInfo/Number", issue => book.IndexNumber = issue);
            hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/Summary", summary => book.Overview = summary);
            hasFoundMetadata |= ReadIntInto(xml, "ComicInfo/Year", year => book.ProductionYear = year);
            hasFoundMetadata |= ReadThreePartDateInto(xml, "ComicInfo/Year", "ComicInfo/Month", "ComicInfo/Day", dateTime => book.PremiereDate = dateTime);
            hasFoundMetadata |= ReadCommaSeperatedStringsInto(xml, "ComicInfo/Genre", genres =>
            {
                foreach (var genre in genres)
                {
                    book.AddGenre(genre);
                }
            });
            hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/Publisher", publisher => book.SetStudios(new[] { publisher }));

            return hasFoundMetadata ? book : null;
        }

        /// <inheritdoc />
        public void ReadPeopleMetadata(XDocument xdocument, MetadataResult<Book> metadataResult)
        {
            ReadCommaSeperatedStringsInto(xdocument, "ComicInfo/Writer", authors =>
            {
                foreach (var author in authors)
                {
                    var person = new PersonInfo { Name = author, Type = PersonKind.Author };
                    metadataResult.AddPerson(person);
                }
            });
            ReadCommaSeperatedStringsInto(xdocument, "ComicInfo/Penciller", pencilers =>
            {
                foreach (var penciller in pencilers)
                {
                    var person = new PersonInfo { Name = penciller, Type = PersonKind.Penciller };
                    metadataResult.AddPerson(person);
                }
            });
            ReadCommaSeperatedStringsInto(xdocument, "ComicInfo/Inker", inkers =>
            {
                foreach (var inker in inkers)
                {
                    var person = new PersonInfo { Name = inker, Type = PersonKind.Inker };
                    metadataResult.AddPerson(person);
                }
            });
            ReadCommaSeperatedStringsInto(xdocument, "ComicInfo/Letterer", letterers =>
            {
                foreach (var letterer in letterers)
                {
                    var person = new PersonInfo { Name = letterer, Type = PersonKind.Letterer };
                    metadataResult.AddPerson(person);
                }
            });
            ReadCommaSeperatedStringsInto(xdocument, "ComicInfo/CoverArtist", coverartists =>
            {
                foreach (var coverartist in coverartists)
                {
                    var person = new PersonInfo { Name = coverartist, Type = PersonKind.CoverArtist };
                    metadataResult.AddPerson(person);
                }
            });
            ReadCommaSeperatedStringsInto(xdocument, "ComicInfo/Colourist", colourists =>
            {
                foreach (var colourist in colourists)
                {
                    var person = new PersonInfo { Name = colourist, Type = PersonKind.Colorist };
                    metadataResult.AddPerson(person);
                }
            });
        }

        /// <inheritdoc />
        public bool ReadCultureInfoInto(XDocument xml, string xPath, Action<CultureInfo> commitResult)
        {
            string? culture = null;

            // Try to read into culture string
            if (!ReadStringInto(xml, xPath, value => culture = value))
            {
                return false;
            }

            try
            {
                // Culture cannot be null here as the method would have returned earlier
                commitResult(new CultureInfo(culture!));
                return true;
            }
            catch (Exception)
            {
                // Ignored
                return false;
            }
        }

        private bool ReadStringInto(XDocument xml, string xPath, Action<string> commitResult)
        {
            var resultElement = xml.XPathSelectElement(xPath);
            if (resultElement is not null && !string.IsNullOrWhiteSpace(resultElement.Value))
            {
                commitResult(resultElement.Value);
                return true;
            }

            return false;
        }

        private bool ReadCommaSeperatedStringsInto(XDocument xml, string xPath, Action<IEnumerable<string>> commitResult)
        {
            var resultElement = xml.XPathSelectElement(xPath);
            if (resultElement is not null && !string.IsNullOrWhiteSpace(resultElement.Value))
            {
                try
                {
                    var splits = resultElement.Value.Split(",").Select(p => p.Trim()).ToArray();
                    if (splits is null || splits.Length < 1)
                    {
                        return false;
                    }

                    commitResult(splits);

                    return true;
                }
                catch (Exception)
                {
                    // Nothing to do here except acknowledging
                    return false;
                }
            }

            return false;
        }

        private bool ReadIntInto(XDocument xml, string xPath, Action<int> commitResult)
        {
            var resultElement = xml.XPathSelectElement(xPath);
            if (resultElement is not null && !string.IsNullOrWhiteSpace(resultElement.Value))
            {
                return ParseInt(resultElement.Value, commitResult);
            }

            return false;
        }

        private bool ReadThreePartDateInto(XDocument xml, string yearXPath, string monthXPath, string dayXPath, Action<DateTime> commitResult)
        {
            int year = 0;
            int month = 0;
            int day = 0;
            var parsed = false;

            parsed |= ReadIntInto(xml, yearXPath, num => year = num);
            parsed |= ReadIntInto(xml, monthXPath, num => month = num);
            parsed |= ReadIntInto(xml, dayXPath, num => day = num);

            // Apparently there were some values inside if this does not return
            if (!parsed)
            {
                return false;
            }

            // Try-Catch because DateTime actually wants a real date, how boring
            try
            {
                var dateTime = new DateTime(year, month, day);
                commitResult(dateTime);
                return true;
            }
            catch (Exception)
            {
                // Nothing to do here except acknowledging
                return false;
            }
        }

        private bool ParseInt(string input, Action<int> commitResult)
        {
            if (int.TryParse(input, out var parsed))
            {
                commitResult(parsed);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
