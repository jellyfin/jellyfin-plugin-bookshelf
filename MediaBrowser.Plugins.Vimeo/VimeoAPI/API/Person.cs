using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Vimeo.API
{
    public class Person
    {
        public string created_on;
        public string id = "-1";
        public bool is_contact;
        public bool is_plus;
        public bool is_staff;
        public bool is_subscribed_to;

        public string username;
        public string display_name;
        public string location;
        public string url;
        public string bio;

        public int number_of_contacts;
        public int number_of_uploads;
        public int number_of_likes;
        public int number_of_videos;
        public int number_of_videos_appears_in;
        public int number_of_albums;
        public int number_of_channels;
        public int number_of_groups;

        public string profileurl;
        public string videosurl;

        public List<Thumbnail> portraits;

        public static Person FromElement(XElement e)
        {
            Person p = new Person();
            try
            {
                p.created_on = e.Attribute("created_on").Value;
                p.id = e.Attribute("id").Value;
                p.is_contact = e.Attribute("is_contact").Value == "1";
                p.is_plus = e.Attribute("is_plus").Value == "1";
                p.is_staff = e.Attribute("is_staff").Value == "1";
                p.is_subscribed_to = e.Attribute("is_subscribed_to").Value == "1";
                p.username = e.Element("username").Value;
                p.display_name = e.Element("display_name").Value;
                p.location = e.Element("location").Value;
                p.url = e.Element("url").Value;
                p.bio = e.Element("bio").Value;
                p.number_of_contacts = int.Parse(e.Element("number_of_contacts").Value);
                p.number_of_uploads = int.Parse(e.Element("number_of_uploads").Value);
                p.number_of_videos = int.Parse(e.Element("number_of_videos").Value);
                p.number_of_videos_appears_in = int.Parse(e.Element("number_of_videos_appears_in").Value);
                p.number_of_albums = int.Parse(e.Element("number_of_albums").Value);
                p.number_of_channels = int.Parse(e.Element("number_of_channels").Value);
                p.number_of_groups = int.Parse(e.Element("number_of_groups").Value);
                p.number_of_likes = int.Parse(e.Element("number_of_likes").Value);
                p.profileurl = e.Element("profileurl").Value;
                p.videosurl = e.Element("videosurl").Value;

                p.portraits = GetPortraits(e.Element("portraits"));
            }
            catch
            {  }
            return p;
        }

        public static List<Thumbnail> GetPortraits(XElement e)
        {
            var portraits = new List<Thumbnail>();
            foreach (var portrait in e.Elements("portrait").ToList())
            {
                Thumbnail t = new Thumbnail();
                t.Height = int.Parse(portrait.Attribute("height").Value);
                t.Width = int.Parse(portrait.Attribute("width").Value);
                t.Url = portrait.Value;
                portraits.Add(t);
            }
            return portraits;
        }
    }
}
