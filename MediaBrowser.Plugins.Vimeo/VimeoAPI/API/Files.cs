using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Vimeo.API
{
    public class Files : List<File>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Files FromElement(XElement e)
        {
            Files es = new Files
            {
                on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                page = int.Parse(e.Attribute("page").Value),
                perpage = int.Parse(e.Attribute("perpage").Value),
                total = int.Parse(e.Attribute("total").Value)
            };
            foreach (var item in e.Elements("file"))
            {
                es.Add(File.FromElement(item));
            }
            return es;
        }
    }

    public class File
    {
        public string add_date;
        public int downloads;
        public string id;
        public string title;
        public string text;
        public Contact creator;

        public static File FromElement(XElement e)
        {
            return new File
            {
                add_date = e.Attribute("add_date").Value,
                downloads = int.Parse(e.Attribute("downloads").Value),
                id = e.Attribute("id").Value,
                title = e.Attribute("title").Value,
                text = e.Element("text").Value,
                creator = Contact.FromElement(e.Element("creator"))
            };
        }
    }
}
