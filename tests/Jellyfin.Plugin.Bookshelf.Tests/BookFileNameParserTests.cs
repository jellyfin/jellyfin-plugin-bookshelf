using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Bookshelf.Common;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Bookshelf.Tests
{
    public class BookFileNameParserTests
    {
        #region Parse

        [Fact]
        public void Parse_WithNameAndDefaultSeriesName_CorrectlyResetSeriesName()
        {
            var expected = new BookFileNameParserResult
            {
                Name = "Children of Time"
            };

            var result = BookFileNameParser.Parse("Children of Time", "books");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Parse_WithNameAndYear_CorrectlyMatchesFileName()
        {
            var expected = new BookFileNameParserResult
            {
                Name = "Children of Time",
                Year = 2015
            };

            var result = BookFileNameParser.Parse("Children of Time (2015)", "books");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Parse_WithIndexAndName_CorrectlyMatchesFileName()
        {
            var expected = new BookFileNameParserResult
            {
                Name = "Children of Time",
                Index = 1
            };

            var result = BookFileNameParser.Parse("1 - Children of Time", "books");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Parse_WithIndexAndNameInFolder_CorrectlyMatchesFileName()
        {
            var expected = new BookFileNameParserResult
            {
                Name = "Children of Ruin",
                SeriesName = "Children of Time",
                Index = 2
            };

            var result = BookFileNameParser.Parse("2 - Children of Ruin", "Children of Time");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Parse_WithIndexNameAndYear_CorrectlyMatchesFileName()
        {
            var expected = new BookFileNameParserResult
            {
                Name = "Children of Time",
                Year = 2015,
                Index = 1
            };

            var result = BookFileNameParser.Parse("1 - Children of Time (2015)", "books");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Parse_WithComicFormat_CorrectlyMatchesFileName()
        {
            BookFileNameParserResult expected;
            BookFileNameParserResult result;

            // Complete format
            expected = new BookFileNameParserResult
            {
                SeriesName = "Children of Time",
                Year = 2019,
                Index = 2
            };

            result = BookFileNameParser.Parse("Children of Time (2015) #2 (of 3) (2019)", "books");
            Assert.Equal(expected, result);

            // Without series year
            expected = new BookFileNameParserResult
            {
                SeriesName = "Children of Time",
                Year = 2019,
                Index = 2
            };

            result = BookFileNameParser.Parse("Children of Time #2 (of 3) (2019)", "books");
            Assert.Equal(expected, result);

            // Without total count
            expected = new BookFileNameParserResult
            {
                SeriesName = "Children of Time",
                Year = 2019,
                Index = 2
            };

            result = BookFileNameParser.Parse("Children of Time #2 (2019)", "books");
            Assert.Equal(expected, result);

            // With only issue number
            expected = new BookFileNameParserResult
            {
                SeriesName = "Children of Time",
                Index = 2
            };

            result = BookFileNameParser.Parse("Children of Time #2", "books");
            Assert.Equal(expected, result);

            // With only issue number and leading zeroes
            expected = new BookFileNameParserResult
            {
                SeriesName = "Children of Time",
                Index = 2
            };

            result = BookFileNameParser.Parse("Children of Time #002", "books");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Parse_WithGoodreadsFormat_CorrectlyMatchesFileName()
        {
            BookFileNameParserResult expected;
            BookFileNameParserResult result;

            // Goodreads format
            expected = new BookFileNameParserResult
            {
                Name = "Children of Ruin",
                SeriesName = "Children of Time",
                Index = 2
            };

            result = BookFileNameParser.Parse("Children of Ruin (Children of Time, #2)", "books");
            Assert.Equal(expected, result);

            // Goodreads format with year added
            expected = new BookFileNameParserResult
            {
                Name = "Children of Ruin",
                SeriesName = "Children of Time",
                Index = 2,
                Year = 2019
            };

            result = BookFileNameParser.Parse("Children of Ruin (Children of Time, #2) (2019)", "books");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Parse_WithSeriesAndName_OverridesSeriesName()
        {
            var expected = new BookFileNameParserResult
            {
                Name = "Children of Ruin",
                SeriesName = "Children of Time",
                Index = 2,
            };

            var result = BookFileNameParser.Parse("Children of Ruin (Children of Time, #2)", "Adrian Tchaikovsky");
            Assert.Equal(expected, result);
        }

        #endregion

        #region GetComparableString

        [Fact]
        public void GetComparableString_WithSpacers_Success()
        {
            var value = "L'île mystérieuse !";
            var expected = "lîle mystérieuse";

            var result = BookFileNameParser.GetComparableString(value, false);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetComparableString_WithNumerals_Success()
        {
            var value = "Dark Knight III: The Master Race III";
            var expected = "dark knight iii master race 3";

            var result = BookFileNameParser.GetComparableString(value, true);

            Assert.Equal(expected, result);
        }

        #endregion
    }
}
