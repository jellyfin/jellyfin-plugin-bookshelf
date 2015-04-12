using System.Threading;
using EmbyTV.EPGProvider.Responses;
using EmbyTV.GeneralHelpers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;

namespace EmbyTV.EPGProvider
{
    public class SchedulesDirect : IEpgSupplier
    {
        public string username;
        public Headend _lineup;
        private string password;
        private string token;
        private string apiUrl;
        private Dictionary<string, ScheduleDirect.Station> channelPair;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        public bool badPassword = false;


        public SchedulesDirect(string username, string password, Headend lineup, ILogger logger,
            IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            this.username = username;
            this.password = password;
            this._lineup = lineup;
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            apiUrl = "https://json.schedulesdirect.org/20141201";
        }

        public async Task GetToken(CancellationToken cancellationToken)
        {
            if (!String.IsNullOrWhiteSpace(username) && !String.IsNullOrWhiteSpace(password))
            {
                var httpOptions = new HttpRequestOptions()
                {
                    Url = apiUrl + "/token",
                    UserAgent = "Emby-Server",
                    RequestContent = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}",
                    CancellationToken = cancellationToken
                };
                //_logger.Info("Obtaining token from Schedules Direct from addres: " + httpOptions.Url + " with body " +
                            // httpOptions.RequestContent);
                try
                {
                    using (var responce = await _httpClient.Post(httpOptions))
                    {
                        var root = _jsonSerializer.DeserializeFromStream<ScheduleDirect.Token>(responce.Content);
                        if (root.message == "OK")
                        {
                            token = root.token;
                            _logger.Info("Authenticated with Schedules Direct token: " + token);
                            badPassword = false;
                        }
                        else
                        {
                            throw new ApplicationException("Could not authenticate with Schedules Direct Error: " +
                                                           root.message);
                        }
                    }
                }
                catch (HttpException e)
                {
                    _logger.Error("Bad username or password for schedules direct");
                    password = "";
                    token = "";
                    badPassword = true;
                }
            }
            else
            {
                token = "";
            }
        }

        public async Task<IEnumerable<ChannelInfo>> getChannelInfo(IEnumerable<ChannelInfo> channelsInfo,
            CancellationToken cancellationToken)
        {
            await GetToken(cancellationToken);
            if (!String.IsNullOrWhiteSpace(token))
            {
                if (!String.IsNullOrWhiteSpace(_lineup.Id))
                {
                    var httpOptions = new HttpRequestOptionsMod()
                    {
                        Url = apiUrl + "/lineups/" + _lineup.Id,
                        UserAgent = "Emby-Server",
                        Token = token,
                        CancellationToken = cancellationToken
                    };
                    channelPair = new Dictionary<string, ScheduleDirect.Station>();
                    using (var response = await _httpClient.Get(httpOptions))
                    {
                        var root = _jsonSerializer.DeserializeFromStream<ScheduleDirect.Channel>(response);
                        _logger.Info("Found " + root.map.Count() + " channels on the lineup on ScheduleDirect");
                        _logger.Info("Mapping Stations to Channel");
                        foreach (ScheduleDirect.Map map in root.map)
                        {
                            var channel = map.channel ?? (map.atscMajor + "." + map.atscMinor); 
                            //_logger.Info("Found channel: "+channel+" in Schedules Direct");
                            if (!channelPair.ContainsKey(channel) && channel != "0.0")
                            {
                                channelPair.Add(channel.TrimStart('0'),
                                    root.stations.FirstOrDefault(item => item.stationID == map.stationID));
                            }
                        }
                        _logger.Info("Added " + channelPair.Count() + " channels to the dictionary");
                        string channelName;
                        foreach (ChannelInfo channel in channelsInfo)
                        {
                            //  Helper.logger.Info("Modifyin channel " + channel.Number);
                            if (channelPair.ContainsKey(channel.Number))
                            {
                                if (channelPair[channel.Number].logo != null)
                                {
                                    channel.ImageUrl = channelPair[channel.Number].logo.URL;
                                    channel.HasImage = true;
                                }
                                if (channelPair[channel.Number].affiliate != null)
                                {
                                    channelName = channelPair[channel.Number].affiliate;
                                }
                                else
                                {
                                    channelName = channelPair[channel.Number].name;
                                }
                                channel.Name = channelName;
                                //channel.Id = channelPair[channel.Number].stationID;
                            }
                            else
                            {
                                _logger.Info("Schedules Direct doesnt have data for channel: " + channel.Number + " " +
                                             channel.Name);
                            }
                        }
                    }
                }
            }
            return channelsInfo;
        }

        public async Task<IEnumerable<ProgramInfo>> getTvGuideForChannel(string channelNumber, DateTime start,
            DateTime end, CancellationToken cancellationToken)
        {
            await GetToken(cancellationToken);
            if (!String.IsNullOrWhiteSpace(_lineup.Id) && !String.IsNullOrWhiteSpace(token))
            {
                HttpRequestOptionsMod httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/schedules",
                    UserAgent = "Emby-Server",
                    Token = token,
                    CancellationToken = cancellationToken
                };
                _logger.Info("Schedules 1");
                List<string> dates = new List<string>();
                int numberOfDay = 0;
                DateTime lastEntry = start;
                while (lastEntry != end)
                {
                    lastEntry = start.AddDays(numberOfDay);
                    dates.Add(lastEntry.ToString("yyyy-MM-dd"));
                    numberOfDay++;
                }
                _logger.Info("Schedules dates is null?" +
                             (dates != null || dates.All(x => string.IsNullOrWhiteSpace(x))));
                _logger.Info("Date count?" + dates[0]);

                string stationID = channelPair[channelNumber].stationID;
                _logger.Info("Channel ?" + stationID);
                List<ScheduleDirect.RequestScheduleForChannel> requestList =
                    new List<ScheduleDirect.RequestScheduleForChannel>()
                    {
                        new ScheduleDirect.RequestScheduleForChannel()
                        {
                            stationID = stationID,
                            date = dates
                        }
                    };

                _logger.Info("Schedules 3");
                _logger.Info("Request string for schedules is: " + _jsonSerializer.SerializeToString(requestList));
                httpOptions.RequestContent = _jsonSerializer.SerializeToString(requestList);
                _logger.Info("Schedules 5");
                using (var response = await _httpClient.Post(httpOptions))
                {
                    StreamReader reader = new StreamReader(response.Content);
                    string responseString = reader.ReadToEnd();
                    _logger.Info("Schedules 6");
                    responseString = "{ \"days\":" + responseString + "}";
                    var root = _jsonSerializer.DeserializeFromString<ScheduleDirect.Schedules>(responseString);
                    // Helper.logger.Info("Found " + root.Count() + " programs on "+channelNumber +" ScheduleDirect");
                    List<ProgramInfo> programsInfo = new List<ProgramInfo>();
                    httpOptions = new HttpRequestOptionsMod()
                    {
                        Url = apiUrl + "/programs",
                        UserAgent = "Emby-Server",
                        Token = token,
                        CancellationToken = cancellationToken
                    };
                    // httpOptions.SetRequestHeader("Accept-Encoding", "deflate,gzip");
                    httpOptions.EnableHttpCompression = true;
                    string requestBody = "";
                    List<string> programsID = new List<string>();
                    foreach (ScheduleDirect.Day day in root.days)
                    {
                        foreach (ScheduleDirect.Program schedule in day.programs)
                        {
                            programsID.Add(schedule.programID);
                        }
                    }
                    _logger.Info("finish creating dict: ");
                    programsID = programsID.Distinct().ToList();

                    requestBody = "[\"" + string.Join("\", \"", programsID) + "\"]";
                    httpOptions.RequestContent = requestBody;
                    List<string> imageID = new List<string>();
                    using (var innerResponse = await _httpClient.Post(httpOptions))
                    {
                        using (var innerReader = new StreamReader(innerResponse.Content))
                        {
                            responseString = innerReader.ReadToEnd();
                            responseString = "{ \"result\":" + responseString + "}";
                            var programDetails =
                                _jsonSerializer.DeserializeFromString<ScheduleDirect.ProgramDetailsResilt>(
                                    responseString);
                            Dictionary<string, ScheduleDirect.ProgramDetails> programDict =
                                programDetails.result.ToDictionary(p => p.programID, y => y);
                            foreach (var program in programDetails.result)
                            {
                                var imageId = program.programID.Substring(0, 10);
                                if (program.hasImageArtwork && !imageID.Contains(imageId))
                                {
                                    imageID.Add(imageId);
                                }
                            }
                            Dictionary<string, string> imageUrls = new Dictionary<string, string>();
                            foreach (var image in imageID)
                            {
                                var imageIdString = "[\"" + image + "\"]";
                                string programs = String.Join(" ",
                                    programDict.Keys.ToList().FindAll(x => x.Substring(0, 10) == image))
                                    ;
                                _logger.Info("Json for show images = " + imageIdString + " used on prgrams " +
                                             programs);
                                httpOptions = new HttpRequestOptionsMod()
                                {
                                    Url = "https://json.schedulesdirect.org/20141201/metadata/programs/",
                                    UserAgent = "Emby-Server",
                                    CancellationToken = cancellationToken
                                };
                                httpOptions.RequestContent = imageIdString;

                                using (var innerResponse2 = await _httpClient.Post(httpOptions))
                                {
                                    List<ScheduleDirect.Image> images;
                                    images = _jsonSerializer.DeserializeFromStream<List<ScheduleDirect.Image>>(
                                        innerResponse2.Content);
                                    //_logger.Info("Images Response: " + _jsonSerializer.SerializeToString(images));
                                    if (images[0] != null)
                                    {
                                        imageUrls.Add(image, images[0].uri);
                                    }
                                }
                            }
                            foreach (ScheduleDirect.Day day in root.days)
                            {
                                foreach (ScheduleDirect.Program schedule in day.programs)
                                {
                                    _logger.Info("Proccesing Schedule for statio ID " + stationID +
                                                 " which corresponds to channel" + channelNumber + " and program id " +
                                                 schedule.programID);


                                    if (imageUrls.ContainsKey(schedule.programID.Substring(0, 10)))
                                    {
                                        string url;
                                        if (imageUrls[schedule.programID.Substring(0, 10)].Contains("http"))
                                        {
                                            url = imageUrls[schedule.programID.Substring(0, 10)];
                                        }
                                        else
                                        {
                                            url = "https://json.schedulesdirect.org/20140530/image/" +
                                                  imageUrls[schedule.programID.Substring(0, 10)];
                                        }
                                        programDict[schedule.programID].images = url;
                                        _logger.Info("URL for image is : " + programDict[schedule.programID].images);
                                    }

                                    programsInfo.Add(GetProgram(channelNumber, schedule, programDict[schedule.programID]));
                                }
                            }
                            _logger.Info("Finished with TVData");
                            return programsInfo;
                        }
                    }
                }
            }

            return (IEnumerable<ProgramInfo>) new List<ProgramInfo>();
        }

        private ProgramInfo GetProgram(string channel, ScheduleDirect.Program programInfo,
            ScheduleDirect.ProgramDetails details)
        {
            _logger.Info("Show type is: " + (details.showType ?? "No ShowType"));
            DateTime startAt = DateTime.ParseExact(programInfo.airDateTime, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
                CultureInfo.InvariantCulture);
            DateTime endAt = startAt.AddSeconds(programInfo.duration);
            ProgramAudio audioType = ProgramAudio.Stereo;
            bool hdtv = false;
            bool repeat = (programInfo.@new == null);
            string newID = programInfo.programID + "T" + startAt.Ticks + "C" + channel;


            if (programInfo.audioProperties != null)
            {
                if (programInfo.audioProperties.Exists(item => item == "stereo"))
                {
                    audioType = ProgramAudio.Stereo;
                }
                else
                {
                    audioType = ProgramAudio.Mono;
                }
            }

            if ((programInfo.videoProperties != null))
            {
                hdtv = programInfo.videoProperties.Exists(item => item == "hdtv");
            }

            string desc = "";
            if (details.descriptions != null)
            {
                if (details.descriptions.description1000 != null)
                {
                    desc = details.descriptions.description1000[0].description;
                }
                else if (details.descriptions.description100 != null)
                {
                    desc = details.descriptions.description100[0].description;
                }
            }
            ScheduleDirect.Gracenote gracenote;
            string EpisodeTitle = "";
            if (details.metadata != null)
            {
                gracenote = details.metadata.Find(x => x.Gracenote != null).Gracenote;
                if ((details.showType ?? "No ShowType") == "Series")
                {
                    EpisodeTitle = "Season: " + gracenote.season + " Episode: " + gracenote.episode;
                }
                if (details.episodeTitle150 != null)
                {
                    EpisodeTitle = EpisodeTitle + " " + details.episodeTitle150;
                }
            }
            if (details.episodeTitle150 != null)
            {
                EpisodeTitle = EpisodeTitle + " " + details.episodeTitle150;
            }
            bool hasImage = false;
            var imageLink = "";

            if (details.hasImageArtwork)
            {
                hasImage = true;
                imageLink = details.images;
            }


            var info = new ProgramInfo
            {
                ChannelId = channel,
                Id = newID,
                Overview = desc,
                StartDate = startAt,
                EndDate = endAt,
                Genres = new List<string>() {"N/A"},
                Name = details.titles[0].title120 ?? "Unkown",
                OfficialRating = "0",
                CommunityRating = null,
                EpisodeTitle = EpisodeTitle,
                Audio = audioType,
                IsHD = hdtv,
                IsRepeat = repeat,
                IsSeries =
                    ((details.showType ?? "No ShowType") == "Series") ||
                    (details.showType ?? "No ShowType") == "Miniseries",
                ImageUrl = imageLink,
                HasImage = hasImage,
                IsNews = false,
                IsKids = false,
                IsSports =
                    ((details.showType ?? "No ShowType") == "Sports non-event") ||
                    (details.showType ?? "No ShowType") == "Sports event",
                IsLive = false,
                IsMovie =
                    (details.showType ?? "No ShowType") == "Feature Film" ||
                    (details.showType ?? "No ShowType") == "TV Movie" ||
                    (details.showType ?? "No ShowType") == "Short Film",
                IsPremiere = false,
            };
            //logger.Info("Done init");
            if (null != details.originalAirDate)
            {
                info.OriginalAirDate = DateTime.Parse(details.originalAirDate);
            }

            if (details.genres != null)
            {
                info.Genres = details.genres.Where(g => !string.IsNullOrWhiteSpace(g)).ToList();
                info.IsNews = details.genres.Contains("news", StringComparer.OrdinalIgnoreCase);
                info.IsKids = false;
            }
            return info;
        }

        public bool checkExist(object obj)
        {
            if (obj != null)
            {
                return true;
            }
            return false;
        }

        public async Task<List<string>> getLineups(CancellationToken cancellationToken)
        {
            List<string> Lineups = new List<string>();
            await GetToken(cancellationToken);
            if (!String.IsNullOrWhiteSpace(token))
            {
                _logger.Info("Lineups on account ");
                var httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/lineups",
                    UserAgent = "Emby-Server",
                    Token = token,
                    CancellationToken = cancellationToken
                };
                var check = false;
                try
                {
                    using (Stream responce = await _httpClient.Get(httpOptions).ConfigureAwait(false))
                    {
                        var root = _jsonSerializer.DeserializeFromStream<ScheduleDirect.Lineups>(responce);
                        _logger.Info("Lineups on account ");
                        if (root.lineups != null)
                        {
                            foreach (ScheduleDirect.Lineup lineup in root.lineups)
                            {
                                _logger.Info("Lineups ID: " + lineup.lineup);
                                if (lineup.lineup == _lineup.Id)
                                {
                                    check = true;
                                }
                                Lineups.Add(lineup.lineup);
                            }
                            if (!String.IsNullOrWhiteSpace(_lineup.Id) && !check)
                            {
                                await addHeadEnd(cancellationToken);
                            }
                        }
                        else
                        {
                            _logger.Info("No lineups on account");
                            Lineups.Add("");
                        }
                        return Lineups;
                    }
                }
                catch (HttpException e)
                {
                    _logger.Info("No lineups on account");
                }
                if (!String.IsNullOrWhiteSpace(_lineup.Id) && !check)
                {
                    await addHeadEnd(cancellationToken);
                }
            }
            Lineups.Add("");
            return Lineups;
        }

        public async Task<List<Headend>> getHeadends(string zipcode, CancellationToken cancellationToken)
        {
            List<Headend> lineups = new List<Headend>();
            await GetToken(cancellationToken);
            if (!String.IsNullOrWhiteSpace(token) && !String.IsNullOrWhiteSpace(zipcode))
            {
                _logger.Info("Headends on account ");
                var httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/headends?country=USA&postalcode=" + zipcode,
                    UserAgent = "Emby-Server",
                    Token = token,
                    CancellationToken = cancellationToken,
                };
                try
                {
                    using (Stream responce = await _httpClient.Get(httpOptions).ConfigureAwait(false))
                    {
                        var root = _jsonSerializer.DeserializeFromStream<List<ScheduleDirect.Headends>>(responce);
                        _logger.Info("Lineups on account ");
                        if (root != null)
                        {
                            foreach (ScheduleDirect.Headends headend in root)
                            {
                                _logger.Info("Headend: " + headend.headend);
                                foreach (ScheduleDirect.Lineup lineup in headend.lineups)
                                    if (!String.IsNullOrWhiteSpace(lineup.name))
                                    {
                                        _logger.Info("Headend: " + lineup.uri.Substring(18));
                                        lineups.Add(new Headend() {Name = lineup.name, Id = lineup.uri.Substring(18)});
                                    }
                            }
                        }
                        else
                        {
                            _logger.Info("No lineups on account");
                        }
                    }
                }
                catch
                    (Exception)
                {
                    _logger.Error("No lineups on account");
                }
            }
            return lineups;
        }

        public async Task addHeadEnd(CancellationToken cancellationToken)
        {
            await GetToken(cancellationToken);
            if (!String.IsNullOrWhiteSpace(token) && !String.IsNullOrWhiteSpace(_lineup.Id))
            {
                _logger.Info("Adding new LineUp ");
                var httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/lineups/" + _lineup.Id,
                    UserAgent = "Emby-Server",
                    Token = token,
                    CancellationToken = cancellationToken
                };

                using (var response = await _httpClient.SendAsync(httpOptions, "PUT"))
                {
                }
            }
        }
    }
}