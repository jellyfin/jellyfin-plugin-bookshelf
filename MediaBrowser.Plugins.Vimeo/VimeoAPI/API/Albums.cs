using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Vimeo.API
{
    public class Albums : List<Album>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Albums FromElement(XElement e)
        {
            Albums es = new Albums
            {
                on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                page = int.Parse(e.Attribute("page").Value),
                perpage = int.Parse(e.Attribute("perpage").Value),
                total = int.Parse(e.Attribute("total").Value)
            };
            foreach (var item in e.Elements("album"))
            {
                es.Add(Album.FromElement(item));
            }
            return es;
        }
    }

    public class Album
    {
        public string id;
        public string title;
        public string description;
        public string created_on;
        public int total_videos;
        public string url;
        public string video_sort_method;
        public Video thumbnail_video;

        public static Album FromElement(XElement e)
        {
            return new Album
            {
                id = e.Attribute("id").Value,
                title = e.Element("title").Value,
                description = e.Element("description").Value,
                created_on = e.Element("created_on").Value,
                total_videos = int.Parse(e.Element("total_videos").Value),
                url = e.Element("url").Value,
                video_sort_method = e.Element("video_sort_method").Value,
                thumbnail_video = new Video
                {
                    id = e.Element("thumbnail_video").Attribute("id").Value,
                    owner = new Contact
                    {
                        id = e.Element("thumbnail_video").Attribute("owner").Value
                    },
                    title = e.Element("thumbnail_video").Element("title").Value,
                    thumbnails = Video.GetThumbnails(e.Element("thumbnail_video").Element("thumbnails"))
                }
            };
        }
    }
}
