using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Vimeo.API
{
    public class Groups : List<Group>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Groups FromElement(XElement e)
        {
            Groups es = new Groups
            {
                on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                page = int.Parse(e.Attribute("page").Value),
                perpage = int.Parse(e.Attribute("perpage").Value),
                total = int.Parse(e.Attribute("total").Value)
            };
            foreach (var item in e.Elements("group"))
            {
                es.Add(Group.FromElement(item));
            }
            return es;
        }
    }

    public class Group
    {
        /*
        public class Moderators : List<Moderator>
        {
            public int on_this_page;
            public int page;
            public int perpage;
            public int total;

            public static Moderators FromElement(XElement e)
            {
                Moderators cs = new Moderators();
                cs.on_this_page = int.Parse(e.Attribute("on_this_page").Value);
                cs.page = int.Parse(e.Attribute("page").Value);
                cs.perpage = int.Parse(e.Attribute("perpage").Value);
                cs.total = int.Parse(e.Attribute("total").Value);
                foreach (var c in e.Elements("moderator"))
                {
                    cs.Add(Moderator.FromElement(c));
                }
                return cs;
            }
        }

        public class Moderator : Contact
        {
            public string moderator_title;

            public static Moderator FromElement(XElement e)
            {
                Moderator c = new Moderator();
                c.display_name = e.Attribute("display_name").Value;
                c.id = e.Attribute("id").Value;
                c.is_plus = e.Attribute("is_plus").Value == "1";
                c.is_staff = e.Attribute("is_staff").Value == "1";
                c.mutual = e.Attribute("mutual") != null ? (e.Attribute("mutual").Value == "1") : false;
                c.profileurl = e.Attribute("profileurl").Value;
                c.realname = e.Attribute("realname").Value;
                c.username = e.Attribute("username").Value;
                c.videosurl = e.Attribute("videosurl").Value;
                c.portraits = Person.GetPortraits(e.Element("portraits"));
                c.moderator_title = e.Attribute("moderator_title").Value;

                return c;
            }
        }
          */
        public class Permissions
        {
            public bool can_users_apply;
            public string group_type;
            public string who_can_add_vids;
            public string who_can_comment;
            public string who_can_create_events;
            public string who_can_invite;
            public string who_can_upload;
            public string who_can_use_forums;

            public static Permissions FromElement(XElement e)
            {
                return new Permissions
                {
                    can_users_apply = e.Attribute("can_users_apply").Value == "1",
                    group_type = e.Attribute("group_type").Value,
                    who_can_add_vids = e.Attribute("who_can_add_vids").Value,
                    who_can_comment = e.Attribute("who_can_comment").Value,
                    who_can_create_events = e.Attribute("who_can_create_events").Value,
                    who_can_invite = e.Attribute("who_can_invite").Value,
                    who_can_upload = e.Attribute("who_can_upload").Value,
                    who_can_use_forums = e.Attribute("who_can_use_forums").Value
                };
            }
        }
        public string id;
        public bool is_featured;
        public string name;
        public string description;
        public string created_on;
        public string modified_on;

        public int total_videos;
        public int total_members;
        public int total_threads;
        public int total_files;
        public int total_events;

        public string url;
        //public string logo_url;

        public Permissions permissions;

        public string calendar_type;

        public Contact creator;

        public static Group FromElement(XElement e)
        {
            return new Group
            {
                id = e.Attribute("id").Value,
                is_featured = e.Attribute("is_featured").Value == "1",
                name = e.Element("name").Value,
                description = e.Element("description") != null ? e.Element("description").Value : "",
                created_on = e.Element("created_on").Value,
                modified_on = e.Element("modified_on").Value,
                total_videos = int.Parse(e.Element("total_videos").Value),
                total_members = int.Parse(e.Element("total_members").Value),
                total_threads = int.Parse(e.Element("total_threads").Value),
                total_files = int.Parse(e.Element("total_files").Value),
                total_events = int.Parse(e.Element("total_events").Value),
                url = e.Element("url").Value,
                //logo_url = e.Element("logo_url").Value,
                permissions = Permissions.FromElement(e.Element("permissions")),
                calendar_type = e.Element("calendar").Attribute("type").Value,
                creator = Contact.FromElement(e.Element("creator"))
            };
        }
    }
}
