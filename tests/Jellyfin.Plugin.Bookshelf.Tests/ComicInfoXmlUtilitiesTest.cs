using Xunit;
using System;
using System.Xml.Linq;
using System.Globalization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;

using Jellyfin.Plugin.Bookshelf.Providers.ComicInfo;

namespace Jellyfin.Plugin.Bookshelf.Tests;

public class ComicInfoXmlUtilitiesTest
{

    private readonly XDocument _document;
    private readonly ComicInfoXmlUtilities _uut;

    public ComicInfoXmlUtilitiesTest()
    {
        _document = GenerateTestData();
        _uut = new ComicInfoXmlUtilities();
    }

    [Fact]
    public void ReadTitle_Success()
    {
        var actual = _uut.ReadComicBookMetadata(_document);
        Assert.NotNull(actual);
        Assert.Equal("The Desperate Battle Begins!", actual.Name);
    }

    [Fact]
    public void ReadAlternativeSeries_Success()
    {
        // Based on the The Anansi Project, some US comics can be part of cross-over
        // story arcs. This field is used to specify an alternate series
        // https://anansi-project.github.io/docs/comicinfo/documentation#alternateseries--alternatenumber--alternatecount
        // However, software like ComicTagger (https://github.com/comictagger/comictagger) uses
        // this field for the series name in the original language when tagging manga
        var actual = _uut.ReadComicBookMetadata(_document);
        Assert.NotNull(actual);
        Assert.Equal("進撃の巨人", actual.OriginalTitle);
    }

    [Fact]
    public void ReadSeries_Success()
    {
        var actual = _uut.ReadComicBookMetadata(_document);
        Assert.NotNull(actual);
        Assert.Equal("Attack on Titan", actual.SeriesName);
    }

    [Fact(DisplayName = "Check that the issue equals the index number.")]
    public void ReadNumber_Success()
    {
        var actual = _uut.ReadComicBookMetadata(_document);
        Assert.NotNull(actual);
        Assert.Equal(1, actual.IndexNumber);
    }

    [Fact]
    public void ReadSummary_Success()
    {
        var actual = _uut.ReadComicBookMetadata(_document);
        var expected = "Eren Jaeger lives in city surrounded by monolithic walls. Outside dwell human murdering Titans. For decades members of the " +
            "Scouting Legion have been the only humans who dared to leave the safety of the walls and gather information on the Titans. Every time " +
            "they return, many of them are dead. Freedom loving Eren has no greater wish than to join them. \n\n Chapter TitlesEpisode 1: To You, " +
            "2,000 Years From NowEpisode 2: That DayEpisode 3: Night of the Disbanding CeremonyEpisode 4: First Battle";

        Assert.NotNull(actual);
        Assert.Equal(expected, actual.Overview);
    }

    [Fact]
    public void ReadProductionYear_Success()
    {
        var actual = _uut.ReadComicBookMetadata(_document);
        Assert.NotNull(actual);
        Assert.Equal(2012, actual.ProductionYear);
    }

    [Fact]
    public void ReadDate_Success()
    {
        var actual = _uut.ReadComicBookMetadata(_document);
        var expected = new DateTime(2012, 6, 30);

        Assert.NotNull(actual);
        Assert.Equal(expected, actual.PremiereDate);
    }

    [Fact]
    public void ReadGenres_Success()
    {
        var actual = _uut.ReadComicBookMetadata(_document);
        Assert.NotNull(actual);
        Assert.NotEmpty(actual.Genres);
        Assert.Equal("Action", actual.Genres.GetValue(0));
        Assert.Equal("Dark fantasy", actual.Genres.GetValue(1));
        Assert.Equal("Post-apocalyptic", actual.Genres.GetValue(2));
    }

    [Fact]
    public void ReadPublisher_Success()
    {
        var actual = _uut.ReadComicBookMetadata(_document);
        Assert.NotNull(actual);
        Assert.Single(actual.Studios);
        Assert.Equal("Kodansha Comics USA", actual.Studios.GetValue(0));
    }

    [Fact]
    public void ReadPeopleMetadata_Success()
    {
        var book = new Book();
        var metadataResult = new MetadataResult<Book> { Item = book, HasMetadata = true };

        _uut.ReadPeopleMetadata(_document, metadataResult);

        var author = new PersonInfo { Name = "Hajime Isayama", Type = "Author" };
        var penciller = new PersonInfo { Name = "A Penciller", Type = "Penciller" };
        var inker = new PersonInfo { Name = "An Inker", Type = "Inker" };
        var letterer = new PersonInfo { Name = "Steve Wands", Type = "Letterer" };
        var coverArtist0 = new PersonInfo { Name = "Artist A", Type = "Cover Artist" };
        var coverArtist1 = new PersonInfo { Name = "Takashi Shimoyama", Type = "Cover Artist" };
        var colourist = new PersonInfo { Name = "An Colourist", Type = "Colourist" };

        Assert.Collection(metadataResult.People, authorActual =>
            {
                Assert.Equal(author.Name, authorActual.Name);
                Assert.Equal(author.Type, authorActual.Type);
            },
            pencillerActual =>
            {
                Assert.Equal(penciller.Name, pencillerActual.Name);
                Assert.Equal(penciller.Type, pencillerActual.Type);
            },
            inkerActual =>
            {
                Assert.Equal(inker.Name, inkerActual.Name);
                Assert.Equal(inker.Type, inkerActual.Type);
            },
            lettererActual =>
            {
                Assert.Equal(letterer.Name, lettererActual.Name);
                Assert.Equal(letterer.Type, lettererActual.Type);
            },
            coverArtist0Actual =>
            {
                Assert.Equal(coverArtist0.Name, coverArtist0Actual.Name);
                Assert.Equal(coverArtist0.Type, coverArtist0Actual.Type);
            },
            coverArtist1Actual =>
            {
                Assert.Equal(coverArtist1.Name, coverArtist1Actual.Name);
                Assert.Equal(coverArtist1.Type, coverArtist1Actual.Type);
            },
            colouristActual =>
            {
                Assert.Equal(colourist.Name, colouristActual.Name);
                Assert.Equal(colourist.Type, colouristActual.Type);
            });
    }

    [Fact]
    public void ReadCultureInfoInto_Success()
    {
        var expectedCultureInfo = new CultureInfo("en");

        bool expected = true;
        bool actual = _uut.ReadCultureInfoInto(_document, "ComicInfo/LanguageISO", cultureInfo => Assert.Equal(cultureInfo.CompareInfo, expectedCultureInfo.CompareInfo));

        Assert.True(actual == expected);
    }

    public XDocument GenerateTestData()
    {
        XDocument document = new XDocument(new XDeclaration("1.0", "", ""));

        XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
        XNamespace xsd = XNamespace.Get("http://www.w3.org/2001/XMLSchema");
        XElement comicInfo = new XElement("ComicInfo", new XAttribute(XNamespace.Xmlns + "xsi", xsi), new XAttribute(XNamespace.Xmlns + "xsd", xsd));
        document.Add(comicInfo);

        comicInfo.Add(new XElement("Title", "The Desperate Battle Begins!"));
        comicInfo.Add(new XElement("AlternateSeries", "進撃の巨人"));
        comicInfo.Add(new XElement("Series", "Attack on Titan"));
        comicInfo.Add(new XElement("Number", "1"));
        comicInfo.Add(new XElement("Count", "1"));
        comicInfo.Add(new XElement("Volume", "1"));
        comicInfo.Add(new XElement("Summary", "Eren Jaeger lives in city surrounded by monolithic walls. Outside dwell human murdering Titans. For decades " +
                "members of the Scouting Legion have been the only humans who dared to leave the safety of the walls and gather information on the Titans. " +
                "Every time they return, many of them are dead. Freedom loving Eren has no greater wish than to join them. \n\n Chapter TitlesEpisode 1: " +
                "To You, 2,000 Years From NowEpisode 2: That DayEpisode 3: Night of the Disbanding CeremonyEpisode 4: First Battle"));
        comicInfo.Add(new XElement("Notes", "Tagged with ComicTagger 1.3.0a0 using info from Comic Vine on 2021-07-24 01:15:20.  [Issue ID 342215]"));
        comicInfo.Add(new XElement("Year", "2012"));
        comicInfo.Add(new XElement("Month", "6"));
        comicInfo.Add(new XElement("Day", "30"));
        comicInfo.Add(new XElement("Writer", "Hajime Isayama"));
        comicInfo.Add(new XElement("Penciller", "A Penciller"));
        comicInfo.Add(new XElement("Inker", "An Inker"));
        comicInfo.Add(new XElement("Colourist", "An Colourist"));
        comicInfo.Add(new XElement("Letterer", "Steve Wands"));
        comicInfo.Add(new XElement("CoverArtist", "Artist A, Takashi Shimoyama"));
        comicInfo.Add(new XElement("Publisher", "Kodansha Comics USA"));
        comicInfo.Add(new XElement("Genre", "Action, Dark fantasy, Post-apocalyptic"));
        comicInfo.Add(new XElement("Web", "https://comicvine.gamespot.com/attack-on-titan-1-the-desperate-battle-begins/4000-342215"));
        comicInfo.Add(new XElement("PageCount", "210"));
        comicInfo.Add(new XElement("LanguageISO", "en"));
        comicInfo.Add(new XElement("Format", "Black & White"));
        comicInfo.Add(new XElement("Manga", "Yes"));
        comicInfo.Add(new XElement("Characters", "Annie Leonhart, Armin Arlert, Bertolt Hoover, Carla Yeager, Connie Springer, Eren Yeager, Franz Kefka, " +
                "Grisha Yeager, Hannah Diamant, Hannes, Jean Kirstein, Krista Lenz, Marco Bott, Mikasa Ackerman, Mina Carolina, Reiner Braun, Samuel " +
                "Linke-Jackson, Sasha Blouse, Thomas Wagner"));
        comicInfo.Add(new XElement("Teams", "Titans"));
        comicInfo.Add(new XElement("Locations", "Shiganshina District, Trost District, Wall Maria, Wall Rose"));
        comicInfo.Add(new XElement("ScanInformation", "vol1"));

        // add the cover page and example pages
        XElement pages = new XElement("Pages");
        comicInfo.Add(pages);
        // image size is arbitary chosen instead of using a real image size (in bytes) for each page
        pages.Add(new XElement("Page", new XAttribute("Image", "0"), new XAttribute("Type", "FrontCover"), new XAttribute("ImageSize", "41911")));
        // add the remaining 209 pages, starting from 1, as page 0 has already been added
        for (int i = 1; i <= 210; i++)
        {
            // image size is arbitary chosen instead of using a real image size (in bytes) for each page
            pages.Add(new XElement("Page", new XAttribute("Image", i), new XAttribute("ImageSize", "14922")));
        }

        return document;
    }
}
