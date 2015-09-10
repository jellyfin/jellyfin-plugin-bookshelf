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
using System.Threading;
using System.Threading.Tasks;

namespace EmbyTV.EPGProvider
{
    public class SchedulesDirect : IEpgSupplier
    {
        private string _token;
        private string _tokenUsername;
        private readonly string _apiUrl;
        private Dictionary<string, ScheduleDirect.Station> _channelPair;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly SemaphoreSlim _tokenSemaphore = new SemaphoreSlim(1, 1);
        private const string UserAgent = "EmbyTV";
        public static SchedulesDirect Current;

        public SchedulesDirect(ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _apiUrl = "https://json.schedulesdirect.org/20141201";
            Current = this;
            
        }

        private async Task<string> GetToken(CancellationToken cancellationToken)
        {
            await _tokenSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var username = Plugin.Instance.Configuration.username ?? "";
               
                // Reset the token if there's no username
                if (string.IsNullOrWhiteSpace(username))
                {
                    _token = null;
                    return null;
                }

                // Reset the token if the username has changed
                if (!string.Equals(username, _tokenUsername, StringComparison.OrdinalIgnoreCase))
                {
                    _token = null;
                }


                /* Token can change if another application makes a request have to check if token is still good, so just get a new token.
                if (!string.IsNullOrWhiteSpace(_token))
                {
                    return _token;
                }
                */
                var password = Plugin.Instance.Configuration.hashPassword ?? "";
                if (string.IsNullOrWhiteSpace(password))
                {
                    _token = null;
                    return null;
                }


                var result = await GetTokenInternal(username, password, cancellationToken).ConfigureAwait(false);
                _token = result;
                _tokenUsername = username;
                return result;
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        private async Task<string> GetTokenInternal(string username, string password,
            CancellationToken cancellationToken)
        {
            var httpOptions = new HttpRequestOptions()
            {
                Url = _apiUrl + "/token",
                UserAgent = UserAgent,
                RequestContent = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}",
                CancellationToken = cancellationToken
            };
            //_logger.Info("Obtaining token from Schedules Direct from addres: " + httpOptions.Url + " with body " +
            // httpOptions.RequestContent);

            using (var responce = await _httpClient.Post(httpOptions))
            {
                var root = _jsonSerializer.DeserializeFromStream<ScheduleDirect.Token>(responce.Content);
                if (root.message == "OK")
                {
                    _logger.Info("Authenticated with Schedules Direct token: " + root.token);
                    return root.token;
                }

                throw new ApplicationException("Could not authenticate with Schedules Direct Error: " + root.message);
            }
        }

        public async Task<IEnumerable<ChannelInfo>> getChannelInfo(IEnumerable<ChannelInfo> channelsInfo,
            CancellationToken cancellationToken)
        {
            var token = await GetToken(cancellationToken);

            var lineup = Plugin.Instance.Configuration.lineup;
            _channelPair = new Dictionary<string, ScheduleDirect.Station>();
            if (!String.IsNullOrWhiteSpace(token) && !String.IsNullOrWhiteSpace(lineup.Id))
            {
                    var httpOptions = new HttpRequestOptionsMod()
                    {
                        Url = _apiUrl + "/lineups/" + lineup.Id,
                        UserAgent = UserAgent,
                        Token = token,
                        CancellationToken = cancellationToken
                    };
                   
                    using (var response = await _httpClient.Get(httpOptions))
                    {
                        var root = _jsonSerializer.DeserializeFromStream<ScheduleDirect.Channel>(response);
                        _logger.Info("Found " + root.map.Count() + " channels on the lineup on ScheduleDirect");
                        _logger.Info("Mapping Stations to Channel");
                        foreach (ScheduleDirect.Map map in root.map)
                        {
                            var channel = (map.channel ?? (map.atscMajor + "." + map.atscMinor)).TrimStart('0');
                            _logger.Debug("Found channel: " + channel + " in Schedules Direct");
                            if (!_channelPair.ContainsKey(channel) && channel != "0.0")
                            {
                                _channelPair.Add(channel,
                                    root.stations.FirstOrDefault(item => item.stationID == map.stationID));
                            }
                        }
                        _logger.Info("Added " + _channelPair.Count() + " channels to the dictionary");
                        
                        foreach (ChannelInfo channel in channelsInfo)
                        {
                            //  Helper.logger.Info("Modifyin channel " + channel.Number);
                            if (_channelPair.ContainsKey(channel.Number))
                            {
                                string channelName;
                                if (_channelPair[channel.Number].logo != null)
                                {
                                    channel.ImageUrl = _channelPair[channel.Number].logo.URL;
                                    channel.HasImage = true;
                                }
                                if (_channelPair[channel.Number].affiliate != null)
                                {
                                    channelName = _channelPair[channel.Number].affiliate;
                                }
                                else
                                {
                                    channelName = _channelPair[channel.Number].name;
                                }
                                channel.Name = channelName;
                            }
                            else
                            {
                                _logger.Info("Schedules Direct doesnt have data for channel: " + channel.Number + " " +
                                             channel.Name);
                            }
                        }
                    }
                }
            return channelsInfo;
        }

        public async Task<List<ScheduleDirect.ShowImages>> GetImageForPrograms(List<string> programIds,
            CancellationToken cancellationToken)
        {
            var imageIdString = "[";

            programIds.ForEach(i =>
            {
                if (!imageIdString.Contains(i.Substring(0, 10)))
                {
                    imageIdString += "\"" + i.Substring(0, 10) + "\",";
                }
                ;
            });
            imageIdString = imageIdString.TrimEnd(',') + "]";
            _logger.Debug("Json for show images = " + imageIdString);
            var httpOptions = new HttpRequestOptionsMod()
            {
                Url = _apiUrl + "/metadata/programs",
                UserAgent = UserAgent,
                CancellationToken = cancellationToken,
                RequestContent = imageIdString
            };
            List<ScheduleDirect.ShowImages> images;
            using (var innerResponse2 = await _httpClient.Post(httpOptions))
            {
                images = _jsonSerializer.DeserializeFromStream<List<ScheduleDirect.ShowImages>>(
                    innerResponse2.Content);
            }

            return images;
        }

        public async Task<IEnumerable<ProgramInfo>> getTvGuideForChannel(string channelNumber, DateTime start,
            DateTime end, CancellationToken cancellationToken)
        {
            List<ProgramInfo> programsInfo = new List<ProgramInfo>();
            if (_channelPair.ContainsKey(channelNumber))
            {
                var token = await GetToken(cancellationToken);

                if (string.IsNullOrWhiteSpace(token))
                {
                    return programsInfo;
                }

                HttpRequestOptionsMod httpOptions = new HttpRequestOptionsMod()
                {
                    Url = _apiUrl + "/schedules",
                    UserAgent = UserAgent,
                    Token = token,
                    CancellationToken = cancellationToken
                };
                List<string> dates = new List<string>();
                int numberOfDay = 0;
                DateTime lastEntry = start;
                while (lastEntry != end)
                {
                    lastEntry = start.AddDays(numberOfDay);
                    dates.Add(lastEntry.ToString("yyyy-MM-dd"));
                    numberOfDay++;
                }
                string stationID = _channelPair[channelNumber].stationID;
                _logger.Info("Channel Station ID is: " + stationID);
                List<ScheduleDirect.RequestScheduleForChannel> requestList =
                    new List<ScheduleDirect.RequestScheduleForChannel>()
                    {
                        new ScheduleDirect.RequestScheduleForChannel()
                        {
                            stationID = stationID,
                            date = dates
                        }
                    };

                var requestString = _jsonSerializer.SerializeToString(requestList);
                _logger.Debug("Request string for schedules is: " + requestString);
                httpOptions.RequestContent = requestString;
                using (var response = await _httpClient.Post(httpOptions))
                {
                    StreamReader reader = new StreamReader(response.Content);
                    string responseString = reader.ReadToEnd();
                    var dailySchedules = _jsonSerializer.DeserializeFromString<List<ScheduleDirect.Day>>(responseString);
                    _logger.Debug("Found " + dailySchedules.Count() + " programs on " + channelNumber +
                                  " ScheduleDirect");
                    httpOptions = new HttpRequestOptionsMod()
                    {
                        Url = _apiUrl + "/programs",
                        UserAgent = UserAgent,
                        Token = token,
                        CancellationToken = cancellationToken
                    };
                    httpOptions.EnableHttpCompression = true;
                    List<string> programsID = new List<string>();
                    programsID = dailySchedules.SelectMany(d => d.programs.Select(s => s.programID)).Distinct().ToList();
                    var requestBody = "[\"" + string.Join("\", \"", programsID) + "\"]";
                    httpOptions.RequestContent = requestBody;

                    using (var innerResponse = await _httpClient.Post(httpOptions))
                    {
                        StreamReader innerReader = new StreamReader(innerResponse.Content);
                        responseString = innerReader.ReadToEnd();
                        var programDetails =
                            _jsonSerializer.DeserializeFromString<List<ScheduleDirect.ProgramDetails>>(
                                responseString);
                        Dictionary<string, ScheduleDirect.ProgramDetails> programDict =
                            programDetails.ToDictionary(p => p.programID, y => y);
                        var images =
                            await
                                GetImageForPrograms(
                                    programDetails.Where(p => p.hasImageArtwork).Select(p => p.programID).ToList(),
                                    cancellationToken);
                        var schedules = dailySchedules.SelectMany(d => d.programs);
                        foreach (ScheduleDirect.Program schedule in schedules)
                        {
                            _logger.Debug("Proccesing Schedule for statio ID " + stationID +
                                          " which corresponds to channel " + channelNumber + " and program id " +
                                          schedule.programID + " which says it has images? " +
                                          programDict[schedule.programID].hasImageArtwork);
                            var imageIndex =
                                images.FindIndex(i => i.programID == schedule.programID.Substring(0, 10));
                            if (imageIndex > -1)
                            {
                                programDict[schedule.programID].images = GetProgramLogo(images[imageIndex]);
                            }
                            programsInfo.Add(GetProgram(channelNumber, schedule, programDict[schedule.programID]));
                        }
                        _logger.Info("Finished with EPGData");
                    }
                }
            }
            return programsInfo;
        }


        private ProgramInfo GetProgram(string channel, ScheduleDirect.Program programInfo,
            ScheduleDirect.ProgramDetails details)
        {
            _logger.Debug("Show type is: " + (details.showType ?? "No ShowType"));
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
            }
            if (details.episodeTitle150 != null)
            {
                EpisodeTitle = EpisodeTitle + " " + details.episodeTitle150;
            }
            bool hasImage = false;
            var imageLink = "";

            if (details.hasImageArtwork)
            {
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
                HasImage = details.hasImageArtwork,
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

        public async Task<List<string>> getLineups(CancellationToken cancellationToken)
        {
            var token = await GetToken(cancellationToken);

            List<string> Lineups = new List<string>();

            if (!String.IsNullOrWhiteSpace(token))
            {
                _logger.Info("Lineups on account ");
                var httpOptions = new HttpRequestOptionsMod()
                {
                    Url = _apiUrl + "/lineups",
                    UserAgent = UserAgent,
                    Token = token,
                    CancellationToken = cancellationToken
                };
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
                                Lineups.Add(lineup.lineup);
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
                catch (Exception e)
                {
                    _logger.Info("No lineups on account");
                }
            }
            Lineups.Add("");
            return Lineups;
        }

        public string GetProgramLogo(ScheduleDirect.ShowImages images)
        {
            string url = "";
            if (images.data != null)
            {
                var smallImages = images.data.Where(i => i.size == "Sm").ToList();
                if (smallImages.Any())
                {
                    images.data = smallImages;
                }
                var logoIndex = images.data.FindIndex(i => i.category == "Logo");
                if (logoIndex == -1)
                {
                    logoIndex = 0;
                }
                if (images.data[logoIndex].uri.Contains("http"))
                {
                    url = images.data[logoIndex].uri;
                }
                else
                {
                    url = _apiUrl + "/image/" + images.data[logoIndex].uri;
                }
                _logger.Debug("URL for image is : " + url);
            }
            return url;
        }

        public async Task<List<Headend>> GetHeadends(string zipcode, CancellationToken cancellationToken)
        {
            var token = await GetToken(cancellationToken);

            var lineups = new List<Headend>();

            if (string.IsNullOrWhiteSpace(token))
            {
                return lineups;
            }

            _logger.Info("Headends on account ");
            var httpOptions = new HttpRequestOptionsMod()
            {
                Url = _apiUrl + "/headends?country=USA&postalcode=" + zipcode,
                UserAgent = UserAgent,
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
                            {
                                _logger.Info("Headend: " + lineup.uri.Substring(18));
                                lineups.Add(new Headend()
                                {
                                    Name = string.IsNullOrWhiteSpace(lineup.name) ? lineup.lineup : lineup.name,
                                    Id = lineup.uri.Substring(18)
                                });
                            }
                        }
                    }
                    else
                    {
                        _logger.Info("No lineups on account");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting headends", ex);
            }

            return lineups;
        }


        public async Task addHeadEnd(string id, CancellationToken cancellationToken)
        {
            var token = await GetToken(cancellationToken);

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Authentication required.");
            }

            _logger.Info("Adding new LineUp ");

            var httpOptions = new HttpRequestOptionsMod()
            {
                Url = _apiUrl + "/lineups/" + id,
                UserAgent = UserAgent,
                Token = token,
                CancellationToken = cancellationToken
            };

            using (var response = await _httpClient.SendAsync(httpOptions, "PUT"))
            {
            }
        }
    }
}