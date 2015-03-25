using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.EmbyTV.GuideData.Responses
{
    class ScheduleDirect
    {
        public class Token
        {
            public int code { get; set; }
            public string message { get; set; }
            public string serverID { get; set; }
            public string token { get; set; }
        }
        public class Lineup
        {
            public string lineup { get; set; }
            public string name { get; set; }
            public string transport { get; set; }
            public string location { get; set; }
            public string uri { get; set; }
        }

        public class Lineups
        {
            public int code { get; set; }
            public string serverID { get; set; }
            public string datetime { get; set; }
            public List<Lineup> lineups { get; set; }
        }


        public class Headends
        {
            public string headend { get; set; }
            public string transport { get; set; }
            public string location { get; set; }
            public List<Lineup> lineups { get; set; }
        }



        public class Channel
        {
            public Map[] map { get; set; }
            public Station[] stations { get; set; }
            public Metadata metadata { get; set; }
        }

        public class Metadata
        {
            public string lineup { get; set; }
            public DateTime modified { get; set; }
            public string transport { get; set; }
        }

        public class Map
        {
            public string stationID { get; set; }
            public string channel { get; set; }
        }

        public class Station
        {
            public string callsign { get; set; }
            public string name { get; set; }
            public string broadcastLanguage { get; set; }
            public string descriptionLanguage { get; set; }
            public string stationID { get; set; }
            public Logo logo { get; set; }
            public string affiliate { get; set; }
            public bool isCommercialFree { get; set; }
        }

        public class Logo
        {
            public string URL { get; set; }
            public int height { get; set; }
            public int width { get; set; }
            public string md5 { get; set; }
        }

        public class RequestScheduleForChannel
        {
            public string stationID { get; set; }
            public List<string> date { get; set; }
        }




        public class Rating
        {
            public string body { get; set; }
            public string code { get; set; }
        }

        public class Multipart
        {
            public int partNumber { get; set; }
            public int totalParts { get; set; }
        }

        public class Program
        {
            public string programID { get; set; }
            public string airDateTime { get; set; }
            public int duration { get; set; }
            public string md5 { get; set; }
            public List<string> audioProperties { get; set; }
            public List<string> videoProperties { get; set; }
            public List<Rating> ratings { get; set; }
            public bool? @new { get; set; }
            public Multipart multipart { get; set; }
        }



        public class MetadataSchedule
        {
            public string modified { get; set; }
            public string md5 { get; set; }
            public string startDate { get; set; }
            public string endDate { get; set; }
            public int days { get; set; }
        }

        public class Day
        {
            public string stationID { get; set; }
            public List<Program> programs { get; set; }
            public MetadataSchedule metadata { get; set; }
        }


        public class Schedules
        {
            public List<Day> days { get; set; }
        }












        public class Title
        {
            public string title120 { get; set; }
        }

        public class EventDetails
        {
            public string subType { get; set; }
        }

        public class Description100
        {
            public string descriptionLanguage { get; set; }
            public string description { get; set; }
        }

        public class Description1000
        {
            public string descriptionLanguage { get; set; }
            public string description { get; set; }
        }

        public class DescriptionsProgram
        {
            public List<Description100> description100 { get; set; }
            public List<Description1000> description1000 { get; set; }
        }

        public class Gracenote
        {
            public int season { get; set; }
            public int episode { get; set; }
        }

        public class MetadataPrograms
        {
            public Gracenote Gracenote { get; set; }
        }

        public class ContentRating
        {
            public string body { get; set; }
            public string code { get; set; }
        }

        public class Cast
        {
            public string billingOrder { get; set; }
            public string role { get; set; }
            public string nameId { get; set; }
            public string personId { get; set; }
            public string name { get; set; }
            public string characterName { get; set; }
        }

        public class Crew
        {
            public string billingOrder { get; set; }
            public string role { get; set; }
            public string nameId { get; set; }
            public string personId { get; set; }
            public string name { get; set; }
        }

        public class QualityRating
        {
            public string ratingsBody { get; set; }
            public string rating { get; set; }
            public string minRating { get; set; }
            public string maxRating { get; set; }
            public string increment { get; set; }
        }

        public class Movie
        {
            public string year { get; set; }
            public int duration { get; set; }
            public List<QualityRating> qualityRating { get; set; }
        }

        public class Recommendation
        {
            public string programID { get; set; }
            public string title120 { get; set; }
        }

        public class ProgramDetails
        {
            public string programID { get; set; }
            public List<Title> titles { get; set; }
            public EventDetails eventDetails { get; set; }
            public DescriptionsProgram descriptions { get; set; }
            public string originalAirDate { get; set; }
            public List<string> genres { get; set; }
            public string episodeTitle150 { get; set; }
            public List<MetadataPrograms> metadata { get; set; }
            public List<ContentRating> contentRating { get; set; }
            public List<Cast> cast { get; set; }
            public List<Crew> crew { get; set; }
            public string showType { get; set; }
            public bool hasImageArtwork { get; set; }
            public string images { get; set; }
            public string imageID { get; set; }
            public string md5 { get; set; }
            public List<string> contentAdvisory { get; set; }
            public Movie movie { get; set; }
            public List<Recommendation> recommendations { get; set; }
        }

        public class ProgramDetailsResilt
        {
            public List<ProgramDetails> result { get; set; }
        }

        public class Caption
        {
            public string content { get; set; }
            public string lang { get; set; }
        }

        public class Image
        {
            public string uri { get; set; }
            public string height { get; set; }
            public string width { get; set; }
            public string primary { get; set; }
            public string category { get; set; }
            public string tier { get; set; }
            public string md5 { get; set; }
            public Caption caption { get; set; }
        }
        public class Result
        {
            public List<Image> images { get; set; }
        }

        public class ShowImages
        {
            public List<Result> results { get; set; }
        }
    }
}


