using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace MediaBrowser.Plugins.Vimeo.VimeoAPI.API
{
    public class Videos : List<Video>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Videos FromElement(XElement e, bool full_response)
        {
            var vs = new Videos
            {
                on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                page = int.Parse(e.Attribute("page").Value),
                perpage = int.Parse(e.Attribute("perpage").Value),
                total = int.Parse(e.Attribute("total").Value)
            };
            vs.AddRange(e.Elements("video").Select(s => Video.FromElement(s, full_response)));
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
                var u = new CastMember
                {
                    display_name = e.Attribute("display_name").Value,
                    id = e.Attribute("id").Value,
                    role = e.Attribute("role").Value,
                    username = e.Attribute("username").Value
                };
                return u;
            }

            public static CastMember FromElementFull(XElement e)
            {
                var c = new CastMember
                {
                    display_name = e.Attribute("display_name").Value,
                    id = e.Attribute("id").Value,
                    is_plus = e.Attribute("is_plus").Value == "1",
                    is_staff = e.Attribute("is_staff").Value == "1",
                    profileurl = e.Attribute("profileurl").Value,
                    realname = e.Attribute("realname").Value,
                    role = e.Attribute("role").Value,
                    username = e.Attribute("username").Value,
                    videosurl = e.Attribute("videosurl").Value,
                    portraits = Person.GetPortraits(e.Element("portraits"))
                };
                return c;
            }
        }

        public class Url
        {
            public string type;
            public string Value;

            public static Url FromElement(XElement e)
            {
                var u = new Url {type = e.Attribute("type").Value, Value = e.Value};
                return u;
            }
        }

        public class Liker : Contact
        {
            public string liked_on;

            public static new Liker FromElement(XElement e)
            {
                var c = new Liker
                {
                    display_name = e.Attribute("display_name").Value,
                    id = e.Attribute("id").Value,
                    is_plus = e.Attribute("is_plus").Value == "1",
                    is_staff = e.Attribute("is_staff").Value == "1",
                    liked_on = e.Attribute("liked_on").Value,
                    profileurl = e.Attribute("profileurl").Value,
                    realname = e.Attribute("realname").Value,
                    username = e.Attribute("username").Value,
                    videosurl = e.Attribute("videosurl").Value,
                    portraits = Person.GetPortraits(e.Element("portraits"))
                };
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
                var cs = new Likers
                {
                    on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                    page = int.Parse(e.Attribute("page").Value),
                    perpage = int.Parse(e.Attribute("perpage").Value),
                    total = int.Parse(e.Attribute("total").Value)
                };
                cs.AddRange(e.Elements("user").Select(Liker.FromElement));
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
                var t = new Tag();
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
            return e.Elements("thumbnail").Select(portrait => new Thumbnail
            {
                Height = int.Parse(portrait.Attribute("height").Value), Width = int.Parse(portrait.Attribute("width").Value), Url = portrait.Value
            }).ToList();
        }

        public static Video FromElement(XElement e, bool full_response)
        {
            var v = new Video();
            if (full_response)
            {
                try
                {
                    v.embed_privacy = e.Attribute("embed_privacy").Value;
                    v.id = e.Attribute("id").Value;
                    v.is_hd = e.Attribute("is_hd").Value == "1";
                    //v.is_transcoding = e.Attribute("is_transcoding").Value;
                    //v.is_watchlater = e.Attribute("is_watchlater") == null ? false : e.Attribute("is_watchlater").Value == "1";
                    v.license = e.Attribute("license").Value;
                    v.privacy = e.Attribute("privacy").Value;

                    v.title = e.Element("title").Value;
                    v.description = e.Element("description").Value;
                    v.upload_date = e.Element("upload_date").Value;
                    v.modified_date = e.Element("modified_date").Value;

                    //if (string.IsNullOrEmpty(e.Element("number_of_likes").Value))
                       // return v;

                    v.number_of_likes = int.Parse(e.Element("number_of_likes").Value);
                    /*if (!string.IsNullOrEmpty(e.Element("number_of_plays").Value))
                    {
                        v.number_of_plays = int.Parse(e.Element("number_of_plays").Value);
                    }
                    else
                    {
                        v.number_of_plays = 100;
                    }*/
                    //v.number_of_comments = int.Parse(e.Element("number_of_comments").Value);
                    v.width = int.Parse(e.Element("width").Value);
                    v.height = int.Parse(e.Element("height").Value);
                    v.duration = int.Parse(e.Element("duration").Value);

                    v.owner = Contact.FromElement(e.Element("owner"));

                    /*v.cast = new List<CastMember>();
                    foreach (var item in e.Element("cast").Elements("member"))
                    {
                        v.cast.Add(CastMember.FromElement(item));
                    }*/

                    v.urls = new List<Url>();
                    foreach (var item in e.Elements("urls").Elements("url"))
                    {
                        v.urls.Add(Url.FromElement(item));
                    }

                    v.thumbnails = GetThumbnails(e.Element("thumbnails"));

                    /*v.tags = new List<Tag>();
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
                    catch (Exception ex)
                    {
                        //Debug.WriteLine("snazy - " + ex);
                    }*/
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine("snazy2 - " + ex);
                }
                return v;
            }

            v.embed_privacy = e.Attribute("embed_privacy").Value;
            v.id = e.Attribute("id").Value;
            v.is_hd = e.Attribute("is_hd").Value == "1";
            v.is_watchlater = e.Attribute("is_watchlater") != null && e.Attribute("is_watchlater").Value == "1";
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
