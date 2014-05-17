using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Vimeo.API
{
    public class Topics : List<Topic>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Topics FromElement(XElement e)
        {
            Topics es = new Topics
            {
                on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                page = int.Parse(e.Attribute("page").Value),
                perpage = int.Parse(e.Attribute("perpage").Value),
                total = int.Parse(e.Attribute("total").Value)
            };
            foreach (var item in e.Elements("topic"))
            {
                es.Add(Topic.FromElement(item));
            }
            return es;
        }
    }

    public class Topic
    {
        public string created_on;
        public string id;
        public bool locked;
        public string name;
        public bool sticky;
        public Contact creator;

        public static Topic FromElement(XElement e)
        {
            return new Topic
            {
                created_on = e.Attribute("created_on").Value,
                id = e.Attribute("id").Value,
                locked = e.Attribute("locked").Value == "1",
                name = e.Attribute("name").Value,
                sticky = e.Attribute("sticky").Value == "1",
                creator = Contact.FromElement(e.Element("creator"))
            };
        }
    }
}
