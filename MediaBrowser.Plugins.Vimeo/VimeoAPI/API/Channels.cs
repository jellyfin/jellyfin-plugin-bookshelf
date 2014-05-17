using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Vimeo.API
{
    public class Channels : List<Channel>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Channels FromElement(XElement e)
        {
            Channels es = new Channels
            {
                on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                page = int.Parse(e.Attribute("page").Value),
                perpage = int.Parse(e.Attribute("perpage").Value),
                total = int.Parse(e.Attribute("total").Value)
            };
            foreach (var item in e.Elements("channel"))
            {
                es.Add(Channel.FromElement(item));
            }
            return es;
        }
    }

    public class Channel
    {
        public string id;
        public bool is_featured;
        public bool is_sponsored;

        public string name;
        public string description;
        public string created_on;
        public string modified_on;

        public int total_videos;
        public int total_subscribers;

        public string logo_url;
        public string badge_url;
        public string url;

        public string layout;
        public string theme;
        public string privacy;

        //string featured_description_short;
        //string featured_description;

        public Contact creator;

        public class Moderators : List<Moderator>
        {
            public int on_this_page;
            public int page;
            public int perpage;
            public int total;

            public static Moderators FromElement(XElement e)
            {
                Moderators es = new Moderators
                {
                    on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                    page = int.Parse(e.Attribute("page").Value),
                    perpage = int.Parse(e.Attribute("perpage").Value),
                    total = int.Parse(e.Attribute("total").Value)
                };
                foreach (var item in e.Elements("subscriber"))
                {
                    es.Add(Moderator.FromElement(item));
                }
                return es;
            }
        }

        public class Moderator : Contact
        {
            
            public bool is_creator;

            public new static Moderator FromElement(XElement e)
            {
                //Subscriber m = (Subscriber)(Contact.FromElement(e));
                Moderator c = new Moderator();
                c.display_name = e.Attribute("display_name").Value;
                c.id = e.Attribute("id").Value;
                c.is_plus = e.Attribute("is_plus").Value == "1";
                c.is_staff = e.Attribute("is_staff").Value == "1";
                c.profileurl = e.Attribute("profileurl").Value;
                c.realname = e.Attribute("realname").Value;
                c.username = e.Attribute("username").Value;
                c.videosurl = e.Attribute("videosurl").Value;
                c.portraits = Person.GetPortraits(e.Element("portraits"));
                c.is_creator = e.Attribute("is_creator").Value == "1";
                return c;
            }
        }
    
        public static Channel FromElement(XElement e)
        {
            return new Channel
            {
                id = e.Attribute("id").Value,
                is_featured = e.Attribute("is_featured").Value == "1",
                is_sponsored = e.Attribute("is_sponsored").Value == "1",
                name = e.Element("name").Value,
                description = e.Element("description").Value,
                created_on = e.Element("created_on").Value,
                modified_on = e.Element("modified_on").Value,
                total_videos = int.Parse(e.Element("total_videos").Value),
                total_subscribers = int.Parse(e.Element("total_subscribers").Value),
                logo_url = e.Element("logo_url").Value,
                badge_url = e.Element("badge_url").Value,
                url = e.Element("url").Value,
                layout = e.Element("layout").Value,
                theme = e.Element("theme").Value,
                privacy = e.Element("privacy").Value,
                //featured_description_short = e.Element("featured_description").Attribute("short").Value,
                //featured_description = e.Element("featured_description").Value,
                creator = Contact.FromElement(e.Element("creator"))
            };
        }
    }
}
