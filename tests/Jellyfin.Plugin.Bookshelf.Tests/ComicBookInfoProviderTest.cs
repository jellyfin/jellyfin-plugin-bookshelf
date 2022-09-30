using System;
using Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Bookshelf.Tests;

public class ComicBookInfoProviderTest
{
    private readonly ComicBookInfoFormat _comicBookInfoFormat;

    private readonly ComicBookInfoProvider _uut;

    public ComicBookInfoProviderTest()
    {
        _comicBookInfoFormat = GenerateTestData();

        var fileSystem = Substitute.For<IFileSystem>();
        var logger = Substitute.For<ILogger<ComicBookInfoProvider>>();
        _uut = new ComicBookInfoProvider(fileSystem, logger);
    }

    [Fact]
    public void ReadTitle_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = _uut.ReadComicBookMetadata(_comicBookInfoFormat.Metadata!);
        Assert.NotNull(actual);
        Assert.Equal("At Midnight, All the Agents", actual!.Name);
    }

    [Fact(DisplayName = "Check that the series has no alternative title.")]
    public void ReadAlternativeSeries_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = _uut.ReadComicBookMetadata(_comicBookInfoFormat.Metadata!);
        Assert.NotNull(actual);
        Assert.Null(actual!.OriginalTitle);
    }

    [Fact]
    public void ReadSeries_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = _uut.ReadComicBookMetadata(_comicBookInfoFormat.Metadata!);
        Assert.NotNull(actual);
        Assert.Equal("Watchmen", actual!.SeriesName);
    }

    [Fact(DisplayName = "Check that the issue equals the index number.")]
    public void ReadNumber_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = _uut.ReadComicBookMetadata(_comicBookInfoFormat.Metadata!);
        Assert.NotNull(actual);
        Assert.Equal(1, actual!.IndexNumber);
    }

    [Fact]
    public void ReadSummary_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = _uut.ReadComicBookMetadata(_comicBookInfoFormat.Metadata!);
        var expected = "Tales of the Black Freighter...";

        Assert.NotNull(actual);
        Assert.Equal(expected, actual!.Overview);
    }

    [Fact]
    public void ReadProductionYear_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = _uut.ReadComicBookMetadata(_comicBookInfoFormat.Metadata!);
        Assert.NotNull(actual);
        Assert.Equal(1986, actual!.ProductionYear);
    }

    [Fact]
    public void ReadDate_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = _uut.ReadComicBookMetadata(_comicBookInfoFormat.Metadata!);
        var expected = new DateTime(1986, 9, 1);

        Assert.NotNull(actual);
        Assert.Equal(expected, actual!.PremiereDate);
    }

    [Fact]
    public void ReadGenres_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = _uut.ReadComicBookMetadata(_comicBookInfoFormat.Metadata!);
        Assert.NotNull(actual);
        Assert.Single(actual!.Genres);
        Assert.Equal("Superhero", actual!.Genres.GetValue(0));
    }

    [Fact]
    public void ReadPublisher_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = _uut.ReadComicBookMetadata(_comicBookInfoFormat.Metadata!);
        Assert.NotNull(actual);
        Assert.Single(actual!.Studios);
        Assert.Equal("DC Comics", actual!.Studios.GetValue(0));
    }

    [Fact]
    public void ReadPeopleMetadata_Success()
    {
        var book = new Book();
        var metadataResult = new MetadataResult<Book> { Item = book, HasMetadata = true };

        Assert.NotNull(_comicBookInfoFormat.Metadata);
        _uut.ReadPeopleMetadata(_comicBookInfoFormat.Metadata!, metadataResult);

        var writer = new PersonInfo { Name = "Alan Moore", Type = "Writer" };
        var artist = new PersonInfo { Name = "Dave Gibbons", Type = "Artist" };
        var letterer = new PersonInfo { Name = "Dave Gibbons", Type = "Letterer" };
        var colorer = new PersonInfo { Name = "John Gibbons", Type = "Colorer" };
        var editor0 = new PersonInfo { Name = "Len Wein", Type = "Editor" };
        var editor1 = new PersonInfo { Name = "Barbara Kesel", Type = "Editor" };
        var example = new PersonInfo { Name = "Takashi Shimoyama", Type = "Example" };

        Assert.Collection(
            metadataResult.People,
            writerActual =>
            {
                Assert.Equal(writer.Name, writerActual.Name);
                Assert.Equal(writer.Type, writerActual.Type);
            },
            artistActual =>
            {
                Assert.Equal(artist.Name, artistActual.Name);
                Assert.Equal(artist.Type, artistActual.Type);
            },
            lettererActual =>
            {
                Assert.Equal(letterer.Name, lettererActual.Name);
                Assert.Equal(letterer.Type, lettererActual.Type);
            },
            colorerActual =>
            {
                Assert.Equal(colorer.Name, colorerActual.Name);
                Assert.Equal(colorer.Type, colorerActual.Type);
            },
            editor0Actual =>
            {
                Assert.Equal(editor0.Name, editor0Actual.Name);
                Assert.Equal(editor0.Type, editor0Actual.Type);
            },
            editor1Actual =>
            {
                Assert.Equal(editor1.Name, editor1Actual.Name);
                Assert.Equal(editor1.Type, editor1Actual.Type);
            },
            exampleActual =>
            {
                Assert.Equal(example.Name, exampleActual.Name);
                Assert.Equal(example.Type, exampleActual.Type);
            });
    }

    [Fact]
    public void ReadTags_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);
        var actual = _uut.ReadComicBookMetadata(_comicBookInfoFormat.Metadata!);

        var tags = new string[] { "Rorschach", "Ozymandias", "Nite Owl" };

        Assert.NotNull(actual);
        Assert.NotEmpty(actual!.Tags);
        Assert.Collection(
            actual!.Tags,
            tag0 => Assert.Equal(tags[0], tag0),
            tag1 => Assert.Equal(tags[1], tag1),
            tag2 => Assert.Equal(tags[2], tag2));
    }

    // [Fact]
    // public void ReadCultureInfoInto_Success()
    // {
    // Assert.NotNull(_comicBookInfoFormat.Metadata);
    // Assert.NotNull(_comicBookInfoFormat.Metadata!.Language);
    // var actualCultureInfo = _uut.ReadCultureInfoAsThreeLetterIsoInto(_comicBookInfoFormat.Metadata!.Language!);

    // Assert.NotNull(actualCultureInfo);
    // Console.WriteLine("language name: " + new CultureInfo("English").DisplayName);
    // Console.WriteLine("two letter name: " + new CultureInfo("English").Get.TwoLetterISOLanguageName);
    // Console.WriteLine("{0,-31}{1,-47}{2,-25}", "ThreeLetterISOLanguageName", new CultureInfo("English").ThreeLetterISOLanguageName, new CultureInfo("English").ThreeLetterISOLanguageName);
    // Assert.Equal("en", actualCultureInfo);
    // }

    public ComicBookInfoFormat GenerateTestData()
    {
        // example data taken from https://code.google.com/archive/p/comicbookinfo/wikis/Example.wiki
        var credits = new ComicBookInfoCredit[]
        {
            new ComicBookInfoCredit { Person = "Moore, Alan", Role = "Writer" },
            new ComicBookInfoCredit { Person = "Gibbons, Dave", Role = "Artist" },
            new ComicBookInfoCredit { Person = "Gibbons, Dave", Role = "Letterer" },
            new ComicBookInfoCredit { Person = "Gibbons, John", Role = "Colorer" },
            new ComicBookInfoCredit { Person = "Wein, Len", Role = "Editor" },
            new ComicBookInfoCredit { Person = "Kesel, Barbara", Role = "Editor" },
            // example of a non-comma-separated name
            new ComicBookInfoCredit { Person = "Takashi Shimoyama", Role = "Example" }
        };
        var tags = new string[] { "Rorschach", "Ozymandias", "Nite Owl" };
        var infoMetadata = new ComicBookInfoMetadata
        {
            Series = "Watchmen",
            Title = "At Midnight, All the Agents",
            Publisher = "DC Comics",
            PublicationMonth = 9,
            PublicationYear = 1986,
            Issue = 1,
            NumberOfIssues = 12,
            Volume = 1,
            NumberOfVolumes = 1,
            Rating = 5,
            Genre = "Superhero",
            Language = "English",
            Country = "United States",
            Credits = credits,
            Tags = tags,
            Comments = "Tales of the Black Freighter...",
        };
        var infoFormat = new ComicBookInfoFormat
        {
            AppId = "ComicBookLover/888",
            LastModified = "2009-10-25 14:51:31 +0000",
            Metadata = infoMetadata
        };

        return infoFormat;
    }
}
