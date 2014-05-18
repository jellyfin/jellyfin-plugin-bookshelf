using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MediaBrowser.Plugins.Vimeo.VimeoAPI.API
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
            var cs = new Contacts
            {
                on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                page = int.Parse(e.Attribute("page").Value),
                perpage = int.Parse(e.Attribute("perpage").Value),
                total = int.Parse(e.Attribute("total").Value)
            };
            cs.AddRange(e.Elements(elementName).Select(Contact.FromElement));
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
            return new Contact
            {
                display_name = e.Attribute("display_name").Value,
                id = e.Attribute("id").Value,
                is_plus = e.Attribute("is_plus").Value == "1",
                is_staff = e.Attribute("is_staff").Value == "1",
                mutual = e.Attribute("mutual") != null && (e.Attribute("mutual").Value == "1"),
                profileurl = e.Attribute("profileurl").Value,
                realname = e.Attribute("realname").Value,
                username = e.Attribute("username").Value,
                videosurl = e.Attribute("videosurl").Value,
                portraits = Person.GetPortraits(e.Element("portraits"))
            };
        }
    }
}
