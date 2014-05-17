using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Vimeo.API
{
    public class Videos : List<Video>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Videos FromElement(XElement e, bool full_response)
        {
            Videos vs = new Videos();
            vs.on_this_page = int.Parse(e.Attribute("on_this_page").Value);
            vs.page = int.Parse(e.Attribute("page").Value);
            vs.perpage = int.Parse(e.Attribute("perpage").Value);
            vs.total = int.Parse(e.Attribute("total").Value);
            foreach (var s in e.Elements("video"))
            {
                vs.Add(Video.FromElement(s,full_response));
            }
            return vs;
        }
    }

    public class Video
    {
        public class CastMember
        {
            public string display_name;
            public string id;
            public string role;
            public string username;

            public bool is_plus;
            public bool is_staff;
            public bool is_creator;
            public string profileurl;
            public string realname;
            public string videosurl;

            public List<Thumbnail> portraits;

            public static CastMember FromElement(XElement e)
            {
                CastMember u = new CastMember();
                u.display_name = e.Attribute("display_name").Value;
                u.id = e.Attribute("id").Value;
                u.role = e.Attribute("role").Value;
                u.username = e.Attribute("username").Value;
                return u;
            }

            public static CastMember FromElementFull(XElement e)
            {
                CastMember c = new CastMember();
                c.display_name = e.Attribute("display_name").Value;
                c.id = e.Attribute("id").Value;
                c.is_plus = e.Attribute("is_plus").Value == "1";
                c.is_staff = e.Attribute("is_staff").Value == "1";
                c.profileurl = e.Attribute("profileurl").Value;
                c.realname = e.Attribute("realname").Value;
                c.role = e.Attribute("role").Value;
                c.username = e.Attribute("username").Value;
                c.videosurl = e.Attribute("videosurl").Value;
                c.portraits = Person.GetPortraits(e.Element("portraits"));
                return c;
            }
        }

        public class Url
        {
            public string type;
            public string Value;

            public static Url FromElement(XElement e)
            {
                Url u = new Url();
                u.type = e.Attribute("type").Value;
                u.Value = e.Value;
                return u;
            }
        }

        public class Liker : Contact
        {
            public string liked_on;

            public static new Liker FromElement(XElement e)
            {
                Liker c = new Liker();
                c.display_name = e.Attribute("display_name").Value;
                c.id = e.Attribute("id").Value;
                c.is_plus = e.Attribute("is_plus").Value == "1";
                c.is_staff = e.Attribute("is_staff").Value == "1";
                c.liked_on = e.Attribute("liked_on").Value;
                c.profileurl = e.Attribute("profileurl").Value;
                c.realname = e.Attribute("realname").Value;
                c.username = e.Attribute("username").Value;
                c.videosurl = e.Attribute("videosurl").Value;
                c.portraits = Person.GetPortraits(e.Element("portraits"));
                return c;
            }
        }

        public class Likers : List<Liker>
        {
            public int on_this_page;
            public int page;
            public int perpage;
            public int total;

            public static Likers FromElement(XElement e)
            {
                Likers cs = new Likers();
                cs.on_this_page = int.Parse(e.Attribute("on_this_page").Value);
                cs.page = int.Parse(e.Attribute("page").Value);
                cs.perpage = int.Parse(e.Attribute("perpage").Value);
                cs.total = int.Parse(e.Attribute("total").Value);
                foreach (var c in e.Elements("user"))
                {
                    cs.Add(Liker.FromElement(c));
                }
                return cs;
            }
        }

        public class Tag
        {
            public string author;
            public string id;
            public string normalized;
            public string url;
            public string title;

            public static Tag FromElement(XElement e)
            {
                Tag t = new Tag();
                try
                {
                    t.author = e.Attribute("author").Value;
                    t.id = e.Attribute("id").Value;
                    t.normalized = e.Attribute("normalized").Value;
                    t.url = e.Attribute("url").Value;
                    t.title = e.Value;
                }
                catch
                {
                }
                return t;
            }
        }

        public string embed_privacy;
        public string id;
        public bool is_hd;
        public string is_transcoding;
        public bool is_watchlater;
        public string license;
        public string privacy;

        public string title;
        public string description;
        public string upload_date;
        public string modified_date;

        public int number_of_likes;
        public int number_of_plays;
        public int number_of_comments;

        public int width;
        public int height;
        public int duration;

        public Contact owner;
        public List<CastMember> cast;
        public List<Url> urls;
        public List<Thumbnail> thumbnails;
        public List<Tag> tags;

        public static List<Thumbnail> GetThumbnails(XElement e)
        {
            var thumbnails = new List<Thumbnail>();
            foreach (var portrait in e.Elements("thumbnail"))
            {
                Thumbnail t = new Thumbnail();
                t.Height = int.Parse(portrait.Attribute("height").Value);
                t.Width = int.Parse(portrait.Attribute("width").Value);
                t.Url = portrait.Value;
                thumbnails.Add(t);
            }
            return thumbnails;
        }

        public static Video FromElement(XElement e, bool full_response)
        {
            Video v = new Video();
            if (full_response)
            {
                try
                {
                    v.embed_privacy = e.Attribute("embed_privacy").Value;
                    v.id = e.Attribute("id").Value;
                    v.is_hd = e.Attribute("is_hd").Value == "1";
                    v.is_transcoding = e.Attribute("is_transcoding").Value;
                    v.is_watchlater = e.Attribute("is_watchlater") == null ? false : e.Attribute("is_watchlater").Value == "1";
                    v.license = e.Attribute("license").Value;
                    v.privacy = e.Attribute("privacy").Value;

                    v.title = e.Element("title").Value;
                    v.description = e.Element("description").Value;
                    v.upload_date = e.Element("upload_date").Value;
                    v.modified_date = e.Element("modified_date").Value;

                    if (string.IsNullOrEmpty(e.Element("number_of_likes").Value))
                        return v;

                    v.number_of_likes = int.Parse(e.Element("number_of_likes").Value);
                    v.number_of_plays = int.Parse(e.Element("number_of_plays").Value);
                    v.number_of_comments = int.Parse(e.Element("number_of_comments").Value);
                    v.width = int.Parse(e.Element("width").Value);
                    v.height = int.Parse(e.Element("height").Value);
                    v.duration = int.Parse(e.Element("duration").Value);

                    v.owner = Contact.FromElement(e.Element("owner"));

                    v.cast = new List<CastMember>();
                    foreach (var item in e.Element("cast").Elements("member"))
                    {
                        v.cast.Add(CastMember.FromElement(item));
                    }

                    v.urls = new List<Url>();
                    foreach (var item in e.Elements("urls").Elements("url"))
                    {
                        v.urls.Add(Url.FromElement(item));
                    }

                    v.thumbnails = GetThumbnails(e.Element("thumbnails"));

                    v.tags = new List<Tag>();
                    try
                    {
                        if (e.Element("tags") != null && e.Element("tags").Elements("tag") != null)
                        {
                            foreach (var item in e.Element("tags").Elements("tag"))
                            {
                                v.tags.Add(Tag.FromElement(item));
                            }
                        }
                    }
                    catch
                    { }
                }
                catch
                { }
                return v;
            }

            v.embed_privacy = e.Attribute("embed_privacy").Value;
            v.id = e.Attribute("id").Value;
            v.is_hd = e.Attribute("is_hd").Value == "1";
            v.is_watchlater = e.Attribute("is_watchlater") == null ? false : e.Attribute("is_watchlater").Value == "1";
            v.license = e.Attribute("license").Value;
            v.modified_date = e.Attribute("modified_date").Value;
            v.owner = new Contact()
            {
                id = e.Attribute("owner").Value
            };
            v.privacy = e.Attribute("privacy").Value;
            v.title = e.Attribute("title").Value;
            v.upload_date = e.Attribute("upload_date").Value;
            return v;
        }
    }

    
}
