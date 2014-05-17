using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Vimeo.API
{
    public class Activities : List<Activity>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Activities FromElement(XElement e)
        {
            Activities es = new Activities
            {
                on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                page = int.Parse(e.Attribute("page").Value),
                perpage = int.Parse(e.Attribute("perpage").Value),
                total = int.Parse(e.Attribute("total").Value)
            };
            foreach (var item in e.Elements("activity"))
            {
                es.Add(Activity.FromElement(item));
            }
            return es;
        }
    }

    public class Activity
    {
        public class Forum
        {
            public string name;
            public string url;
            public string thread_id;
            public string thread_title;
            public string thread_url;
        }
        public string id;
        public string type;
        public string time;
        public Contact active_user;
        public Forum forum;
        public Video video;
        public Comment comment;
        public Group group;
        public Channel channel;

        public static Activity FromElement(XElement e)
        {
            var a = new Activity
            {
                id = e.Attribute("id").Value,
                type = e.Element("type").Value,
                time = e.Element("time").Value,
                active_user = Contact.FromElement(e.Element("active_user")),
                
            };
            if (e.Element("video") != null)
            {
                a.video = new Video
                {
                    id = e.Element("video").Attribute("id").Value,
                    title = e.Element("video").Element("title").Value,
                    urls = new List<Video.Url>()
                    {
                        new Video.Url{
                            type="video",
                            Value=e.Element("video").Element("url").Value
                        }
                    },
                    owner = Contact.FromElement(e.Element("video").Element("owner")),
                    thumbnails = Video.GetThumbnails(e.Element("video").Element("thumbnails"))
                };
            }
            if (e.Element("forum") != null)
            {
                a.forum = new Forum
                {
                    name = e.Element("forum").Attribute("name").Value,
                    url = e.Element("forum").Attribute("url").Value,
                    thread_id = e.Element("forum").Element("thread").Attribute("id").Value,
                    thread_title = e.Element("forum").Element("thread").Element("title").Value,
                    thread_url = e.Element("forum").Element("thread").Element("url").Value
                };
            }
            if (e.Element("comment") != null)
            {
                a.comment = Comment.FromElement(e.Element("comment"));
            }
            if (e.Element("group") != null)
            {
                a.group = new Group
                {
                    id = e.Element("group").Attribute("id").Value,
                    name = e.Element("group").Element("name").Value,
                    url = e.Element("group").Element("url").Value, 
                    description = e.Element("group").Element("image").Value
                };
            }
            if (e.Element("channel") != null)
            {
                a.channel = new Channel
                {
                    id = e.Element("channel").Attribute("id").Value,
                    name = e.Element("channel").Element("name").Value,
                    url = e.Element("channel").Element("url").Value,
                    logo_url = e.Element("channel").Element("image").Value
                };
            }
            return a;
        }
    }
}
