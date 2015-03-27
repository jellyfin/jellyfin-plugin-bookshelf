using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Globalization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Net;
using MediaBrowser.Common.Net;
using EmbyTV.General_Helper;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.LiveTv;
using EmbyTV.EPGProvider.Responses;
using EmbyTV.GeneralHelpers;


namespace EmbyTV.EPGProvider
{
    public class SchedulesDirect : IEpgSupplier
    {
        public string username;
        public string _lineup;
        private string password;
        private string token;
        private string apiUrl;
        private Dictionary<string, ScheduleDirect.Station> channelPair;

        public SchedulesDirect(string username, string password, string lineup)
        {
            this.username = username;
            this.password = password;
            this._lineup = lineup;
            apiUrl = "https://json.schedulesdirect.org/20141201";
        }
        public async Task getToken(PluginHelper Helper)
        {

            if (username.Length > 0 && password.Length > 0)
            {
                Helper.httpOptions = new HttpRequestOptions()
                {
                    Url = apiUrl + "/token",
                    UserAgent = "Emby-Server",
                    RequestContent = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}",
                };
                Helper.useCancellationToken();
                Helper.logger.Info("[EmbyTV] Obtaining token from Schedules Direct from addres: " + Helper.httpOptions.Url + " with body " + Helper.httpOptions.RequestContent);
                try
                {
                    Stream responce = await Helper.Post();
                    var root = Helper.jsonSerializer.DeserializeFromStream<ScheduleDirect.Token>(responce);
                    if (root.message == "OK") { token = root.token; Helper.logger.Info("[EmbyTV] Authenticated with Schedules Direct token: " + token); }
                    else { Helper.logger.Error("[EmbyTV] Could not authenticate with Schedules Direct Error: " + root.message); }
                }
                catch
                {
                    Helper.logger.Error("[EmbyTV] Could not authenticate with Schedules Direct");
                }
            }
        }

        public async Task refreshToken(PluginHelper Helper)
        {

            if (username.Length > 0 && password.Length > 0)
            {
                Helper.httpOptions = new HttpRequestOptions()
                {
                    Url = apiUrl + "/token",
                    UserAgent = "Emby-Server",
                    RequestContent = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}",
                };
                Helper.useCancellationToken();
                Helper.logger.Info("[EmbyTV] Obtaining token from Schedules Direct from addres: " + Helper.httpOptions.Url + " with body " + Helper.httpOptions.RequestContent);
                try
                {
                    Stream responce = await Helper.Post();
                    var root = Helper.jsonSerializer.DeserializeFromStream<ScheduleDirect.Token>(responce);
                    if (root.message == "OK") { token = root.token; Helper.logger.Info("[EmbyTV] Authenticated with Schedules Direct token: " + token); }
                    else { Helper.logger.Error("[EmbyTV] Could not authenticate with Schedules Direct Error: " + root.message); }
                }
                catch
                {
                    Helper.logger.Error("[EmbyTV] Could not authenticate with Schedules Direct");
                }
            }
        }


        public async Task<IEnumerable<ChannelInfo>> getChannelInfo(PluginHelper Helper, IEnumerable<ChannelInfo> channelsInfo)
        {
            if (username.Length > 0 && password.Length > 0)
            {
                if (apiUrl != "https://json.schedulesdirect.org/20141201") { apiUrl = "https://json.schedulesdirect.org/20141201"; await refreshToken(Helper); }
                else { await getToken(Helper); }
                if (!String.IsNullOrWhiteSpace(_lineup))
                {
                    Helper.httpOptions = new HttpRequestOptionsMod()
                    {
                        Url = apiUrl + "/lineups/" + _lineup,
                        UserAgent = "Emby-Server",
                        Token = token
                    };
                    channelPair = new Dictionary<string, ScheduleDirect.Station>();
                    var response = await Helper.Get();
                    var root = Helper.jsonSerializer.DeserializeFromStream<ScheduleDirect.Channel>(response);
                    Helper.logger.Info("[EmbyTV] Found " + root.map.Count() + " channels on the lineup on ScheduleDirect");
                    Helper.logger.Info("[EmbyTV] Mapping Stations to Channel");
                    foreach (ScheduleDirect.Map map in root.map)
                    {                 
                        channelPair.Add(map.channel.TrimStart('0'), root.stations.First(item => item.stationID == map.stationID));
                    }
                    Helper.LogInfo("Added " + channelPair.Count() + " channels to the dictionary");
                    string channelName;
                    foreach (ChannelInfo channel in channelsInfo)
                    {
                        //  Helper.logger.Info("[EmbyTV] Modifyin channel " + channel.Number);
                        if (channelPair.ContainsKey(channel.Number.TrimStart('0')))
                        {
                            if (channelPair[channel.Number].logo != null) { channel.ImageUrl = channelPair[channel.Number].logo.URL; channel.HasImage = true; }
                            if (channelPair[channel.Number].affiliate != null) { channelName = channelPair[channel.Number].affiliate; }
                            else { channelName = channelPair[channel.Number].name; }
                            channel.Name = channelName;
                            //channel.Id = channelPair[channel.Number].stationID;
                        }
                        else { Helper.LogInfo("Schedules Direct doesnt have data for channel: " + channel.Number + " " + channel.Name); }
                    }
                }
            }
            return channelsInfo;
        }

        public async Task<IEnumerable<ProgramInfo>> getTvGuideForChannel(PluginHelper Helper, string channelNumber, DateTime start, DateTime end)
        {
            if (!String.IsNullOrWhiteSpace(_lineup) && username.Length > 0 && password.Length > 0)
            {

                if (apiUrl != "https://json.schedulesdirect.org/20141201") { apiUrl = "https://json.schedulesdirect.org/20141201"; await refreshToken(Helper); }
                else { await getToken(Helper); }
                HttpRequestOptionsMod httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/schedules",
                    UserAgent = "Emby-Server",
                    Token = token
                };
                Helper.logger.Info("[EmbyTV] Schedules 1"); 
                Helper.httpOptions = httpOptions;
                List<string> dates = new List<string>();
                int numberOfDay = 0;
                DateTime lastEntry = start;
                while (lastEntry != end)
                {
                    lastEntry = start.AddDays(numberOfDay);
                    dates.Add(lastEntry.ToString("yyyy-MM-dd"));
                    numberOfDay++;
                }
                Helper.logger.Info("[EmbyTV] Schedules dates is null?" + (dates != null || dates.All(x => string.IsNullOrWhiteSpace(x))));
                Helper.logger.Info("[EmbyTV] Date count?" + dates[0]);
               
                string stationID = channelPair[channelNumber].stationID;
                Helper.logger.Info("[EmbyTV] Channel ?" + stationID);
                List<ScheduleDirect.RequestScheduleForChannel> requestList = 
                    new List<ScheduleDirect.RequestScheduleForChannel>() {
                        new ScheduleDirect.RequestScheduleForChannel() {
                            stationID = stationID, date = dates 
                        } 
                    };

                Helper.logger.Info("[EmbyTV] Schedules 3"); 
                Helper.logger.Info("[EmbyTV] Request string for schedules is: " + Helper.jsonSerializer.SerializeToString(requestList));
                Helper.httpOptions.RequestContent = Helper.jsonSerializer.SerializeToString(requestList);
                Helper.logger.Info("[EmbyTV] Schedules 5"); 
                var response = await Helper.Post();
                StreamReader reader = new StreamReader(response);
                string responseString = reader.ReadToEnd();
                Helper.logger.Info("[EmbyTV] Schedules 6"); 
                responseString = "{ \"days\":" + responseString + "}";
                var root = Helper.jsonSerializer.DeserializeFromString<ScheduleDirect.Schedules>(responseString);
                // Helper.logger.Info("[EmbyTV] Found " + root.Count() + " programs on "+channelNumber +" ScheduleDirect");
                List<ProgramInfo> programsInfo = new List<ProgramInfo>();
                httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/programs",
                    UserAgent = "Emby-Server",
                    Token = token
                };
               // httpOptions.SetRequestHeader("Accept-Encoding", "deflate,gzip");
                Helper.httpOptions.EnableHttpCompression = true;
                Helper.httpOptions = httpOptions;
                string requestBody = "";
                List<string> programsID = new List<string>();
                List<string> imageID = new List<string>();
                Dictionary<string, List<string>> haveImageID = new Dictionary<string, List<string>>();
                foreach (ScheduleDirect.Day day in root.days)
                {
                    foreach (ScheduleDirect.Program schedule in day.programs)
                    {
                        var imageId = schedule.programID.Substring(0, 10);
                        programsID.Add(schedule.programID);
                        imageID.Add(imageId);
                        
                        if (!haveImageID.ContainsKey(imageId))
                        {
                            haveImageID.Add(imageId, new List<string>());
                        }
                        if (!haveImageID[imageId].Contains(schedule.programID))
                        {
                            haveImageID[imageId].Add(schedule.programID);
                        }
                    }
                }
                Helper.logger.Info("[EmbyTV] finish creating dict: ");

                programsID = programsID.Distinct().ToList();
                imageID = imageID.Distinct().ToList();


                requestBody = "[\"" + string.Join("\", \"", programsID) + "\"]";
                Helper.httpOptions.RequestContent = requestBody;
                response = await Helper.Post();
                reader = new StreamReader(response);
                responseString = reader.ReadToEnd();
                responseString = "{ \"result\":" + responseString + "}";
                var programDetails = Helper.jsonSerializer.DeserializeFromString<ScheduleDirect.ProgramDetailsResilt>(responseString);
                Dictionary<string, ScheduleDirect.ProgramDetails> programDict = programDetails.result.ToDictionary(p => p.programID, y => y);
              
              

                foreach (ScheduleDirect.Day day in root.days)
                {
                    foreach (ScheduleDirect.Program schedule in day.programs)
                    {
                         Helper.logger.Info("[EmbyTV] Proccesing Schedule for statio ID " +stationID+" which corresponds to channel" +channelNumber+" and program id "+ schedule.programID);
                        
                        programsInfo.Add(GetProgram(channelNumber, schedule, Helper.logger, programDict[schedule.programID]));
                    }
               } 
                Helper.logger.Info("Finished with TVData");
                return programsInfo;
            }
            else
            {
                return (IEnumerable<ProgramInfo>)new List<ProgramInfo>();
            }
        }
        private ProgramInfo GetProgram(string channel, ScheduleDirect.Program programInfo, ILogger logger, ScheduleDirect.ProgramDetails details)
        {
            DateTime startAt = DateTime.ParseExact(programInfo.airDateTime, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", CultureInfo.InvariantCulture);
            DateTime endAt = startAt.AddSeconds(programInfo.duration);
            ProgramAudio audioType = ProgramAudio.Mono;
            bool hdtv = false;
            bool repeat = (programInfo.@new == null);
            string newID = programInfo.programID + "T" + startAt.Ticks + "C" + channel;

   
            if (programInfo.audioProperties != null) { if (programInfo.audioProperties.Exists(item => item == "stereo")) { audioType = ProgramAudio.Stereo; } else { audioType = ProgramAudio.Mono; } }
          
            if ((programInfo.videoProperties != null)) { hdtv = programInfo.videoProperties.Exists(item => item == "hdtv"); }

            string desc = "";
            if (details.descriptions != null)
            {
                if (details.descriptions.description1000 != null) { desc = details.descriptions.description1000[0].description; }
                else if (details.descriptions.description100 != null) { desc = details.descriptions.description100[0].description; }
            }
            ScheduleDirect.Gracenote gracenote;
            string EpisodeTitle = "";
            if (details.metadata != null)
            {
                gracenote = details.metadata.Find(x => x.Gracenote != null).Gracenote;
                if (details.eventDetails.subType == "Series") { EpisodeTitle = "Season: " + gracenote.season + " Episode: " + gracenote.episode; }
                if (details.episodeTitle150 != null) { EpisodeTitle = EpisodeTitle+" "+details.episodeTitle150; }
            }
            if (details.episodeTitle150 != null) { EpisodeTitle = EpisodeTitle + " " + details.episodeTitle150; }
            bool hasImage = false;
            var imageLink = "";
            /*
            if (!details.hasImageArtwork != null) {
                hasImage = true;
                imageLink = details.images;

            }
             */
            var info = new ProgramInfo
            {
                ChannelId = channel,
                Id = newID,
                Overview = desc,
                StartDate = startAt,
                EndDate = endAt,
                Genres = new List<string>(){"N/A"},
                Name = details.titles[0].title120 ?? "Unkown",
                OfficialRating = "0",
                CommunityRating = null,
                EpisodeTitle = EpisodeTitle,
                Audio = audioType,
                IsHD = hdtv,
                IsRepeat = repeat,
                IsSeries = (details.eventDetails.subType == "Series"),
                ImageUrl = imageLink,
                HasImage = hasImage,
                IsNews = false,
                IsKids = false,
                IsSports = false,
                IsLive = false,
                IsMovie = false,
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
                info.IsMovie = details.genres.Contains("Feature Film", StringComparer.OrdinalIgnoreCase) || (details.movie != null); 
                info.IsKids = false;
                info.IsSports = details.genres.Contains("sports", StringComparer.OrdinalIgnoreCase) ||
                    details.genres.Contains("Sports non-event", StringComparer.OrdinalIgnoreCase) ||
                    details.genres.Contains("Sports event", StringComparer.OrdinalIgnoreCase) ||
                    details.genres.Contains("Sports talk", StringComparer.OrdinalIgnoreCase) ||
                    details.genres.Contains("Sports news", StringComparer.OrdinalIgnoreCase);
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
        public async Task<string> getLineups(PluginHelper Helper)
        {
            if (username.Length > 0 && password.Length > 0)
            {

                apiUrl = "https://json.schedulesdirect.org/20141201";
                await refreshToken(Helper);
                Helper.logger.Info("[EmbyTV] Lineups on account ");
                Helper.httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/lineups",
                    UserAgent = "Emby-Server",
                    Token = token
                };
                Helper.useCancellationToken();
                string Lineups = "";
                var check = false;
                try
                {
                    Stream responce = await Helper.Get().ConfigureAwait(false);
                    var root = Helper.jsonSerializer.DeserializeFromStream<ScheduleDirect.Lineups>(responce);
                    Helper.logger.Info("[EmbyTV] Lineups on account ");
                    if (root.lineups != null)
                    {
                        foreach (ScheduleDirect.Lineup lineup in root.lineups)
                        {
                            Helper.logger.Info("[EmbyTV] Lineups ID: " + lineup.lineup);
                            if (lineup.lineup == _lineup) { check = true; }
                            if (String.IsNullOrWhiteSpace(Lineups))
                            {
                                Lineups = lineup.lineup;
                            }
                            else { Lineups = Lineups + "," + lineup.lineup; }
                        }
                        if (!String.IsNullOrWhiteSpace(_lineup) && !check) { await addHeadEnd(Helper); }
                    }
                    else
                    {
                        Helper.logger.Info("[EmbyTV] No lineups on account");
                    }
                }
                catch
                {
                    Helper.logger.Error("[EmbyTV] Couldn't obtain lineups");
                    return Lineups;
                }
                return Lineups;
            } return "";
        }
        public async Task<Dictionary<string,string>> getHeadends(string zipcode,PluginHelper Helper)
        {
            Dictionary<string, string> lineups = new Dictionary<string, string>();
            if (username.Length > 0 && password.Length > 0)
            {
                apiUrl = "https://json.schedulesdirect.org/20141201";
                await refreshToken(Helper);
                Helper.logger.Info("[EmbyTV] Headends on account ");
                Helper.httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/headends?country=USA&postalcode=" + zipcode,
                    UserAgent = "Emby-Server",
                    Token = token
                };
                Helper.useCancellationToken();                
                try
                {
                    Stream responce = await Helper.Get().ConfigureAwait(false);
                    var root = Helper.jsonSerializer.DeserializeFromStream<List<ScheduleDirect.Headends>>(responce);
                    Helper.logger.Info("[EmbyTV] Lineups on account ");
                    if (root != null)
                    {
                        foreach (ScheduleDirect.Headends headend in root)
                        {
                            Helper.logger.Info("[EmbyTV] Headend: " + headend.headend);
                            foreach (ScheduleDirect.Lineup lineup in headend.lineups)
                                if (!String.IsNullOrWhiteSpace(lineup.name))
                                {
                                    Helper.logger.Info("[EmbyTV] Headend: " + lineup.uri.Substring(18));
                                    lineups.Add(lineup.name, lineup.uri.Substring(18));
                                }
                        }
                    }
                    else
                    {
                        Helper.logger.Info("[EmbyTV] No lineups on account");
                    }
                }
                catch
                {
                    Helper.logger.Error("[EmbyTV] Couldn't obtain lineups");
                    return lineups;
                }
            }
            return lineups;
        }

        public async Task addHeadEnd(PluginHelper Helper)
        {
            if (username.Length > 0 && password.Length > 0 && String.IsNullOrWhiteSpace(_lineup))
            {
                apiUrl = "https://json.schedulesdirect.org/20141201";
                await refreshToken(Helper);
                Helper.logger.Info("[EmbyTV] Adding new LineUp ");
                Helper.httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/lineups/" + _lineup,
                    UserAgent = "Emby-Server",
                    Token = token
                };
                Helper.useCancellationToken();

                await Helper.httpClient.SendAsync(Helper.httpOptions, "PUT");
            }
        }
    }
}





