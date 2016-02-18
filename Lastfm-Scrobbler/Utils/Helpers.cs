namespace LastfmScrobbler.Utils
{
    using MediaBrowser.Controller.Entities.Audio;
    using Models;
    using Resources;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    public static class Helpers
    {
        public static string CreateMd5Hash(string input)
        {
            // Use input string to calculate MD5 hash
            var md5 = MD5.Create();

            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hashBytes  = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            var sb = new StringBuilder();
            
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public static void AppendSignature(ref Dictionary<string, string> data)
        {
            data.Add("api_sig", CreateSignature(data));
        }

        public static int ToTimestamp(DateTime date)
        {
            return Convert.ToInt32((date - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);
        }

        public static DateTime FromTimestamp(double timestamp)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return date.AddSeconds(timestamp).ToLocalTime();
        }

        public static int CurrentTimestamp()
        {
            return ToTimestamp(DateTime.Now);
        }

        public static string DictionaryToQueryString(Dictionary<string, string> data)
        {
            return String.Join("&", data.Where(k => !String.IsNullOrWhiteSpace(k.Value)).Select(kvp => String.Format("{0}={1}", Uri.EscapeUriString(kvp.Key), Uri.EscapeUriString(kvp.Value))));
        }

        private static string CreateSignature(Dictionary<string, string> data)
        {
            var s = new StringBuilder();

            foreach (var item in data.OrderBy(x => x.Key))
                s.Append(String.Format("{0}{1}", item.Key, item.Value));

            //Append seceret
            s.Append(Strings.Keys.LastfmApiSeceret);

            return CreateMd5Hash(s.ToString());
        }

        //The nuget doesn't seem to have GetProviderId for artists
        public static string GetMusicBrainzArtistId(MusicArtist artist)
        {
            string mbArtistId;
            
            if (artist.ProviderIds == null)
            {
                Plugin.Logger.Debug("No provider id: {0}", artist.Name);
                return null;
            }

            if (artist.ProviderIds.TryGetValue("MusicBrainzArtist", out mbArtistId)) 
                return mbArtistId;

            Plugin.Logger.Debug("No MBID: {0}", artist.Name);
            
            return null;
        }

        public static LastfmTrack FindMatchedLastfmSong(List<LastfmTrack> tracks, Audio song)
        {
            return tracks.FirstOrDefault(lastfmTrack => StringHelper.IsLike(song.Name, lastfmTrack.Name));
        }
    }
}
