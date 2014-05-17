using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Vimeo.API
{
    public class Contacts : List<Contact>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Contacts FromElement(XElement e)
        {
            return FromElement(e, "contact");
        }
        public static Contacts FromElement(XElement e, string elementName)
        {
            Contacts cs = new Contacts();
            cs.on_this_page = int.Parse(e.Attribute("on_this_page").Value);
            cs.page = int.Parse(e.Attribute("page").Value);
            cs.perpage = int.Parse(e.Attribute("perpage").Value);
            cs.total = int.Parse(e.Attribute("total").Value);
            foreach (var c in e.Elements(elementName))
            {
                cs.Add(Contact.FromElement(c));
            }
            return cs;
        }
    }

    public class Contact
    {
        public string display_name;
        public string id;
        public bool is_plus;
        public bool is_staff;
        public bool mutual;
        public string profileurl;
        public string realname;
        public string username;
        public string videosurl;
        public List<Thumbnail> portraits;

        public static Contact FromElement(XElement e)
        {
            Contact c = new Contact();
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
            return c;
        }
    }
}
