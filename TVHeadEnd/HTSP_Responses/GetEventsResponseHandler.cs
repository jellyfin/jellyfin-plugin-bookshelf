using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TVHeadEnd.HTSP;

namespace TVHeadEnd.HTSP_Responses
{
    public class GetEventsResponseHandler : HTSResponseHandler
    {
        private volatile Boolean _dataReady = false;

        private readonly DateTime _initialDateTimeUTC = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly DateTime _startDateTimeUtc, _endDateTimeUtc;
        private readonly ILogger _logger;
        private readonly CancellationToken _cancellationToken;

        private readonly List<ProgramInfo> _result;

        public GetEventsResponseHandler(DateTime startDateTimeUtc, DateTime endDateTimeUtc, ILogger logger, CancellationToken cancellationToken)
        {
            _startDateTimeUtc = startDateTimeUtc;
            _endDateTimeUtc = endDateTimeUtc;

            _logger = logger;
            _cancellationToken = cancellationToken;

            _result = new List<ProgramInfo>();
        }

        public void handleResponse(HTSMessage response)
        {
            _logger.Info("[TVHclient] GetEventsResponseHandler.handleResponse: received answer from TVH server\n" + response.ToString()); 

            if (response.containsField("events"))
            {
                IList events = response.getList("events");
                foreach (HTSMessage currEventMessage in events)
                {
                    ProgramInfo pi = new ProgramInfo();

                    if (currEventMessage.containsField("start"))
                    {
                        long currStartTimeUnix = currEventMessage.getLong("start");
                        DateTime currentStartDateTimeUTC = _initialDateTimeUTC.AddSeconds(currStartTimeUnix).ToUniversalTime();
                        int compResult = DateTime.Compare(currentStartDateTimeUTC, _endDateTimeUtc);
                        if (compResult > 0)
                        {
                            _logger.Info("[TVHclient] GetEventsResponseHandler.handleResponse: start value of event larger query stop value - skipping! \n" 
                                + "Query start UTC dateTime: " + _startDateTimeUtc + "\n"
                                + "Query end UTC dateTime:   " + _endDateTimeUtc + "\n"
                                + "Event start UTC dateTime: " + currentStartDateTimeUTC + "\n"
                                + currEventMessage.ToString());
                            continue;
                        }
                        pi.StartDate = currentStartDateTimeUTC;
                    }
                    else
                    {
                        _logger.Info("[TVHclient] GetEventsResponseHandler.handleResponse: no start value for event - skipping! \n" + currEventMessage.ToString());
                        continue;
                    }

                    if (currEventMessage.containsField("stop"))
                    {
                        long currEndTimeUnix = currEventMessage.getLong("stop");
                        DateTime currentEndDateTimeUTC = _initialDateTimeUTC.AddSeconds(currEndTimeUnix).ToUniversalTime();
                        int compResult = DateTime.Compare(currentEndDateTimeUTC, _startDateTimeUtc);
                        if (compResult < 0)
                        {
                            _logger.Info("[TVHclient] GetEventsResponseHandler.handleResponse: stop value of event smaller query start value - skipping! \n"
                                + "Query start UTC dateTime: " + _startDateTimeUtc + "\n"
                                + "Query end UTC dateTime:   " + _endDateTimeUtc + "\n"
                                + "Event start UTC dateTime: " + currentEndDateTimeUTC + "\n"
                                + currEventMessage.ToString());
                            continue;
                        }
                        pi.EndDate = currentEndDateTimeUTC;
                    }
                    else
                    {
                        _logger.Info("[TVHclient] GetEventsResponseHandler.handleResponse: no stop value for event - skipping! \n" + currEventMessage.ToString());
                        continue;
                    }

                    if (currEventMessage.containsField("channelId"))
                    {
                        pi.ChannelId = "" + currEventMessage.getInt("channelId");
                    }

                    if (currEventMessage.containsField("eventId"))
                    {
                        pi.Id = "" + currEventMessage.getInt("eventId");
                    }

                    if (currEventMessage.containsField("title"))
                    {
                        pi.Name = currEventMessage.getString("title");
                    }

                    if (currEventMessage.containsField("description"))
                    {
                        pi.Overview = currEventMessage.getString("description");
                    }

                    if (currEventMessage.containsField("summary"))
                    {
                        pi.EpisodeTitle = currEventMessage.getString("summary");
                    }

                    if (currEventMessage.containsField("firstAired"))
                    {
                        long firstAiredUtcLong = currEventMessage.getLong("firstAired");
                        pi.OriginalAirDate = _initialDateTimeUTC.AddSeconds(firstAiredUtcLong).ToUniversalTime();
                    }

                    if (currEventMessage.containsField("starRating"))
                    {
                        pi.OfficialRating = "" + currEventMessage.getInt("starRating");
                    }

                    if (currEventMessage.containsField("image"))
                    {
                        pi.HasImage = true;
                        pi.ImageUrl = "" + currEventMessage.getString("image");
                    }
                    else
                    {
                        pi.HasImage = false;
                    }

                    if (currEventMessage.containsField("contentType"))
                    {
                        List<string> genres = new List<string>();

                        int contentType = currEventMessage.getInt("contentType");
                        //byte major = (byte)((contentTypeRaw & 0xF0) >> 4);
                        //byte minor = (byte) (contentTypeRaw & 0xF);

                        switch (contentType)
                        {
                            // movie/drama
                            case 0x10:
                                genres.Add("Drama");
                                genres.Add("Movie");
                                pi.IsMovie = true;
                                break;
                            case 0x11:
                                genres.Add("Drama");
                                genres.Add("Movie");
                                genres.Add("Detective");
                                genres.Add("Thriller");
                                pi.IsMovie = true;
                                break;
                            case 0x12:
                                genres.Add("Drama");
                                genres.Add("Movie");
                                genres.Add("Adventure");
                                genres.Add("Western");
                                genres.Add("War");
                                pi.IsMovie = true;
                                break;
                            case 0x13:
                                genres.Add("Drama");
                                genres.Add("Movie");
                                genres.Add("Science Fiction");
                                genres.Add("Fantasy");
                                genres.Add("Horror");
                                pi.IsMovie = true;
                                break;
                            case 0x14:
                                genres.Add("Drama");
                                genres.Add("Movie");
                                genres.Add("Comedy");
                                pi.IsMovie = true;
                                break;
                            case 0x15:
                                genres.Add("Drama");
                                genres.Add("Movie");
                                genres.Add("Soap");
                                genres.Add("Melodrama");
                                genres.Add("Folkloric");
                                pi.IsMovie = true;
                                break;
                            case 0x16:
                                genres.Add("Drama");
                                genres.Add("Movie");
                                genres.Add("Romance");
                                pi.IsMovie = true;
                                break;
                            case 0x17:
                                genres.Add("Drama");
                                genres.Add("Movie");
                                genres.Add("Serious");
                                genres.Add("ClassicalReligion");
                                genres.Add("Historical");
                                pi.IsMovie = true;
                                break;
                            case 0x18:
                                genres.Add("Drama");
                                genres.Add("Movie");
                                genres.Add("Adult Movie");
                                pi.IsMovie = true;
                                break;

                            // news/current affairs
                            case 0x20:
                                genres.Add("News");
                                genres.Add("Current Affairs");
                                pi.IsNews = true;
                                break;
                            case 0x21:
                                genres.Add("News");
                                genres.Add("Current Affairs");
                                genres.Add("Weather Report");
                                pi.IsNews = true;
                                break;
                            case 0x22:
                                genres.Add("News");
                                genres.Add("Current Affairs");
                                genres.Add("Magazine");
                                pi.IsNews = true;
                                break;
                            case 0x23:
                                genres.Add("News");
                                genres.Add("Current Affairs");
                                genres.Add("Documentary");
                                pi.IsNews = true;
                                break;
                            case 0x24:
                                genres.Add("News");
                                genres.Add("Current Affairs");
                                genres.Add("Discussion");
                                genres.Add("Interview");
                                genres.Add("Debate");
                                pi.IsNews = true;
                                break;

                            // show/game show
                            case 0x30:
                                genres.Add("Show");
                                genres.Add("Game Show");
                                break;
                            case 0x31:
                                genres.Add("Show");
                                genres.Add("Game Show");
                                genres.Add("Quiz");
                                genres.Add("Contest");
                                break;
                            case 0x32:
                                genres.Add("Show");
                                genres.Add("Game Show");
                                genres.Add("Variety");
                                break;
                            case 0x33:
                                genres.Add("Show");
                                genres.Add("Game Show");
                                genres.Add("Talk");
                                break;

                            // sports
                            case 0x40:
                                genres.Add("Sports");
                                pi.IsSports = true;
                                break;
                            case 0x41:
                                genres.Add("Sports");
                                genres.Add("Special Event");
                                pi.IsSports = true;
                                break;
                            case 0x42:
                                genres.Add("Sports");
                                genres.Add("Magazine");
                                pi.IsSports = true;
                                break;
                            case 0x43:
                                genres.Add("Sports");
                                genres.Add("Football");
                                genres.Add("Soccer");
                                pi.IsSports = true;
                                break;
                            case 0x44:
                                genres.Add("Sports");
                                genres.Add("Tennis");
                                genres.Add("Squash");
                                pi.IsSports = true;
                                break;
                            case 0x45:
                                genres.Add("Sports");
                                genres.Add("Team Sports");
                                pi.IsSports = true;
                                break;
                            case 0x46:
                                genres.Add("Sports");
                                genres.Add("Athletics");
                                pi.IsSports = true;
                                break;
                            case 0x47:
                                genres.Add("Sports");
                                genres.Add("Motor Sport");
                                pi.IsSports = true;
                                break;
                            case 0x48:
                                genres.Add("Sports");
                                genres.Add("Water Sport");
                                pi.IsSports = true;
                                break;
                            case 0x49:
                                genres.Add("Sports");
                                genres.Add("Winter Sport");
                                pi.IsSports = true;
                                break;
                            case 0x4a:
                                genres.Add("Sports");
                                genres.Add("Equestrian");
                                pi.IsSports = true;
                                break;
                            case 0x4b:
                                genres.Add("Sports");
                                genres.Add("Martial Sports");
                                pi.IsSports = true;
                                break;

                            // childrens/youth
                            case 0x50:
                                genres.Add("Childrens");
                                genres.Add("Youth");
                                pi.IsKids = true;
                                break;
                            case 0x51:
                                genres.Add("Childrens");
                                genres.Add("Youth");
                                genres.Add("Pre-school");
                                pi.IsKids = true;
                                break;
                            case 0x52:
                                genres.Add("Childrens");
                                genres.Add("Youth");
                                genres.Add("Entertainment (6 to 14 year-olds)");
                                pi.IsKids = true;
                                break;
                            case 0x53:
                                genres.Add("Childrens");
                                genres.Add("Youth");
                                genres.Add("Entertainment (10 to 16 year-olds)");
                                pi.IsKids = true;
                                break;
                            case 0x54:
                                genres.Add("Childrens");
                                genres.Add("Youth");
                                genres.Add("Informational");
                                genres.Add("Educational");
                                genres.Add("Schools");
                                pi.IsKids = true;
                                break;
                            case 0x55:
                                genres.Add("Childrens");
                                genres.Add("Youth");
                                genres.Add("Cartoons");
                                genres.Add("Puppets");
                                pi.IsKids = true;
                                break;

                            // music/ballet/dance
                            case 0x60:
                                genres.Add("Music");
                                genres.Add("Ballet");
                                genres.Add("Dance");
                                break;
                            case 0x61:
                                genres.Add("Music");
                                genres.Add("Ballet");
                                genres.Add("Dance");
                                genres.Add("Pop");
                                genres.Add("Rock");
                                break;
                            case 0x62:
                                genres.Add("Music");
                                genres.Add("Ballet");
                                genres.Add("Dance");
                                genres.Add("Serious Music");
                                genres.Add("Classical Music");
                                break;
                            case 0x63:
                                genres.Add("Music");
                                genres.Add("Ballet");
                                genres.Add("Dance");
                                genres.Add("Folk");
                                genres.Add("Traditional Music");
                                break;
                            case 0x64:
                                genres.Add("Music");
                                genres.Add("Ballet");
                                genres.Add("Dance");
                                genres.Add("Jazz");
                                break;
                            case 0x65:
                                genres.Add("Music");
                                genres.Add("Ballet");
                                genres.Add("Dance");
                                genres.Add("Musical");
                                genres.Add("Opera");
                                break;
                            case 0x66:
                                genres.Add("Music");
                                genres.Add("Ballet");
                                genres.Add("Dance");
                                break;

                            // arts/culture
                            case 0x70:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                break;
                            case 0x71:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                genres.Add("Performing Arts");
                                break;
                            case 0x72:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                genres.Add("Fine Arts");
                                break;
                            case 0x73:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                genres.Add("Religion");
                                break;
                            case 0x74:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                genres.Add("Popular Culture");
                                genres.Add("Tradital Arts");
                                break;
                            case 0x75:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                genres.Add("Literature");
                                break;
                            case 0x76:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                genres.Add("Film");
                                genres.Add("Cinema");
                                break;
                            case 0x77:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                genres.Add("Experimantal Film");
                                genres.Add("Video");
                                break;
                            case 0x78:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                genres.Add("Broadcasting");
                                genres.Add("Press");
                                break;
                            case 0x79:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                genres.Add("New Media");
                                break;
                            case 0x7a:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                genres.Add("Magazine");
                                break;
                            case 0x7b:
                                genres.Add("Arts");
                                genres.Add("Culture");
                                genres.Add("Fashion");
                                break;

                            // social/political/economic
                            case 0x80:
                                genres.Add("Social");
                                genres.Add("Political");
                                genres.Add("Economic");
                                break;
                            case 0x81:
                                genres.Add("Social");
                                genres.Add("Political");
                                genres.Add("Economic");
                                genres.Add("Magazin");
                                genres.Add("Report");
                                genres.Add("Documentary");
                                break;
                            case 0x82:
                                genres.Add("Social");
                                genres.Add("Political");
                                genres.Add("Economic");
                                genres.Add("Economics");
                                genres.Add("Social Advisory");
                                break;
                            case 0x83:
                                genres.Add("Social");
                                genres.Add("Political");
                                genres.Add("Economic");
                                genres.Add("Remarkable People");
                                break;

                            // children's youth: educational/science/factual
                            case 0x90:
                                genres.Add("Educational");
                                genres.Add("Science");
                                genres.Add("Factual");
                                pi.IsKids = true;
                                break;
                            case 0x91:
                                genres.Add("Educational");
                                genres.Add("Science");
                                genres.Add("Factual");
                                genres.Add("Nature");
                                genres.Add("Animals");
                                genres.Add("Environment");
                                pi.IsKids = true;
                                break;
                            case 0x92:
                                genres.Add("Educational");
                                genres.Add("Science");
                                genres.Add("Factual");
                                genres.Add("Technology");
                                genres.Add("Natural Sciences");
                                pi.IsKids = true;
                                break;
                            case 0x93:
                                genres.Add("Educational");
                                genres.Add("Science");
                                genres.Add("Factual");
                                genres.Add("Medicine");
                                genres.Add("Physiology");
                                genres.Add("Psychology");
                                pi.IsKids = true;
                                break;
                            case 0x94:
                                genres.Add("Educational");
                                genres.Add("Science");
                                genres.Add("Factual");
                                genres.Add("Foreign Countries");
                                genres.Add("Expeditions");
                                pi.IsKids = true;
                                break;
                            case 0x95:
                                genres.Add("Educational");
                                genres.Add("Science");
                                genres.Add("Factual");
                                genres.Add("Social");
                                genres.Add("Spiritual Sciences");
                                pi.IsKids = true;
                                break;
                            case 0x96:
                                genres.Add("Educational");
                                genres.Add("Science");
                                genres.Add("Factual");
                                genres.Add("Further Education");
                                pi.IsKids = true;
                                break;
                            case 0x97:
                                genres.Add("Educational");
                                genres.Add("Science");
                                genres.Add("Factual");
                                genres.Add("Languages");
                                pi.IsKids = true;
                                break;

                            // leisure hobbies
                            case 0xa0:
                                genres.Add("Leisure");
                                genres.Add("Hobbies");
                                break;
                            case 0xa1:
                                genres.Add("Leisure");
                                genres.Add("Hobbies");
                                genres.Add("Tourism");
                                genres.Add("Travel");
                                break;
                            case 0xa2:
                                genres.Add("Leisure");
                                genres.Add("Hobbies");
                                genres.Add("Handicraft");
                                break;
                            case 0xa3:
                                genres.Add("Leisure");
                                genres.Add("Hobbies");
                                genres.Add("Motoring");
                                break;
                            case 0xa4:
                                genres.Add("Leisure");
                                genres.Add("Hobbies");
                                genres.Add("Fitness");
                                genres.Add("Health");
                                break;
                            case 0xa5:
                                genres.Add("Leisure");
                                genres.Add("Hobbies");
                                genres.Add("Cooking");
                                break;
                            case 0xa6:
                                genres.Add("Leisure");
                                genres.Add("Hobbies");
                                genres.Add("Advertisement");
                                genres.Add("Shopping");
                                break;
                            case 0xa7:
                                genres.Add("Leisure");
                                genres.Add("Hobbies");
                                genres.Add("Gardening");
                                break;

                            // misc
                            case 0xb0:
                                genres.Add("Original Language");
                                break;
                            case 0xb1:
                                genres.Add("Black and White");
                                break;
                            case 0xb2:
                                genres.Add("Unpublished");
                                break;
                            case 0xb3:
                                genres.Add("Live Broadcast");
                                pi.IsLive = true;
                                break;

                            // drama (user defined, specced in the UK "D-Book")
                            case 0xf0:
                                genres.Add("Drama");
                                pi.IsMovie = true;
                                break;
                            case 0xf1:
                                genres.Add("Drama");
                                genres.Add("Detective");
                                genres.Add("Thriller");
                                pi.IsMovie = true;
                                break;
                            case 0xf2:
                                genres.Add("Drama");
                                genres.Add("Adventure");
                                genres.Add("Western");
                                genres.Add("War");
                                pi.IsMovie = true;
                                break;
                            case 0xf3:
                                genres.Add("Drama");
                                genres.Add("Science Fiction");
                                genres.Add("Fantasy");
                                genres.Add("Horror");
                                pi.IsMovie = true;
                                break;
                            case 0xf4:
                                genres.Add("Drama");
                                genres.Add("Commedy");
                                pi.IsMovie = true;
                                break;
                            case 0xf5:
                                genres.Add("Drama");
                                genres.Add("Soap");
                                genres.Add("Melodrama");
                                genres.Add("Folkloric");
                                pi.IsMovie = true;
                                break;
                            case 0xf6:
                                genres.Add("Drama");
                                genres.Add("Romance");
                                break;
                            case 0xf7:
                                genres.Add("Drama");
                                genres.Add("Serious");
                                genres.Add("ClassicalReligion");
                                genres.Add("Historical");
                                pi.IsMovie = true;
                                break;
                            case 0xf8:
                                genres.Add("Drama");
                                genres.Add("Adult");
                                pi.IsMovie = true;
                                break;

                            default:
                                // unused values
                                break;
                        }
                        pi.Genres = genres;
                    }

                    //pi.IsSeries - bool
                    //pi.CommunityRating  - float
                    //pi.IsHD - bool
                    //pi.IsPremiere - bool
                    //pi.IsRepeat - bool
                    //pi.ImagePath - string
                    //pi.Audio - MediaBrowser.Model.LiveTv.ProgramAudio
                    //pi.ProductionYear - int

                    _logger.Info("[TVHclient] GetEventsResponseHandler.handleResponse: add event\n" + currEventMessage.ToString() + "\n" + createPiInfo(pi));

                    _result.Add(pi);
                }
            }
            _dataReady = true;
        }

        private String createPiInfo(ProgramInfo pi)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\n<ProgramInfo>\n");
            sb.Append("  Id:                    " + pi.Id + "\n");
            sb.Append("  StartDate:             " + pi.StartDate + "\n");
            sb.Append("  EndDate:               " + pi.EndDate + "\n");
            sb.Append("  ChannelId:             " + pi.ChannelId + "\n");
            sb.Append("  Name:                  " + pi.Name + "\n");
            sb.Append("  Overview:              " + pi.Overview + "\n");
            sb.Append("  EpisodeTitle:          " + pi.EpisodeTitle + "\n");
            sb.Append("  OriginalAirDate:       " + pi.OriginalAirDate + "\n");
            sb.Append("  OfficialRating:        " + pi.OfficialRating + "\n");
            sb.Append("  HasImage:              " + pi.HasImage + "\n");
            sb.Append("  ImageUrl:              " + pi.ImageUrl + "\n");
            sb.Append("  IsMovie:               " + pi.IsMovie + "\n");
            sb.Append("  IsKids:                " + pi.IsKids + "\n");
            sb.Append("  IsLive:                " + pi.IsLive + "\n");
            sb.Append("  IsNews:                " + pi.IsNews + "\n");
            sb.Append("  IsSports:              " + pi.IsSports + "\n");
            sb.Append("  Genres:\n");
            List<string> genres = pi.Genres;
            foreach(string currGenres in genres)
            {
              sb.Append("  --> " + currGenres + "\n");
            }
            sb.Append("\n");

            return sb.ToString();
        }

        public Task<IEnumerable<ProgramInfo>> GetEvents(CancellationToken cancellationToken, string channelId)
        {
            return Task.Factory.StartNew<IEnumerable<ProgramInfo>>(() =>
            {
                while (!_dataReady || cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(500);
                }
                _logger.Info("[TVHclient] GetEventsResponseHandler.GetEvents: channelId=" + channelId + "  / dataReady=" + _dataReady + "  / cancellationToken.IsCancellationRequested=" + cancellationToken.IsCancellationRequested);
                return _result;
            });
        }
    }
}
