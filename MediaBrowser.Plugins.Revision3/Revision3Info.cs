using System.Collections.Generic;

namespace MediaBrowser.Plugins.Revision3
{
    public class Images
    {
        public string mini { get; set; }
        public string small { get; set; }
        public string medium { get; set; }
        public string marge { get; set; }
        public string large { get; set; }
        public string logo { get; set; }
        public string logo_100 { get; set; }
        public string logo_160 { get; set; }
        public string logo_200 { get; set; }
        public string leaf { get; set; }
        public string banner { get; set; }
        public string hero { get; set; }
    }

    public class Hd720p30
    {
        public string url { get; set; }
        public int bitrate { get; set; }
        public string filesize { get; set; }
    }

    public class Large
    {
        public string url { get; set; }
        public int bitrate { get; set; }
        public string filesize { get; set; }
    }

    public class Small
    {
        public string url { get; set; }
        public int bitrate { get; set; }
        public string filesize { get; set; }
    }

    public class Media
    {
        public Hd720p30 hd720p30 { get; set; }
        public Large large { get; set; }
        public Small small { get; set; }
    }

    public class Episode
    {
        public string id { get; set; }
        public string show_id { get; set; }
        public string video_id { get; set; }
        public string number { get; set; }
        public string slug { get; set; }
        public string fileroot { get; set; }
        public string name { get; set; }
        public string youtube_name { get; set; }
        public string summary { get; set; }
        public string youtube_summary { get; set; }
        public string description { get; set; }
        public string details { get; set; }
        public string duration { get; set; }
        public string keywords { get; set; }
        public string visibility { get; set; }
        public string published { get; set; }
        public string ad_approved { get; set; }
        public Images images { get; set; }
        public Media media { get; set; }
        public List<object> segments { get; set; }
        public List<object> adslots { get; set; }
        public List<object> campaigns { get; set; }
        public Show show { get; set; }
        public string thread_ident { get; set; }
    }

    public class Category
    {
        public string id { get; set; }
        public string slug { get; set; }
        public string name { get; set; }
    }

    public class Show
    {
        public string id { get; set; }
        public string parent_id { get; set; }
        public string slug { get; set; }
        public string name { get; set; }
        public string tagline { get; set; }
        public string debut { get; set; }
        public string youtube_id { get; set; }
        public string youtube_user { get; set; }
        public string youtube_channel { get; set; }
        public string youtube_channel_id { get; set; }
        public string youtube_sftp { get; set; }
        public string genre { get; set; }
        public string @explicit { get; set; }
        public string visibility { get; set; }
        public string ad_approval_required { get; set; }
        public string youtube_uploader { get; set; }
        public string msn_uploader { get; set; }
        public string summary { get; set; }
        public Images images { get; set; }
        public List<object> domains { get; set; }
        public List<object> campaigns { get; set; }
        public List<Category> categories { get; set; }
        public int nextNum { get; set; }
    }

    public class RootObject
    {
        public int total { get; set; }
        public List<Show> shows { get; set; }

        public List<Episode> episodes { get; set; }
    }
}
