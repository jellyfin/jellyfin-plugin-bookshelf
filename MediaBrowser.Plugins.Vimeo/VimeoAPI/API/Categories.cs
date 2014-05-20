using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MediaBrowser.Plugins.Vimeo.VimeoAPI.API
{
    public class Categories : List<Category>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Categories FromElement(XElement e)
        {
            var es = new Categories
            {
                on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                page = int.Parse(e.Attribute("page").Value),
                perpage = int.Parse(e.Attribute("perpage").Value),
                total = int.Parse(e.Attribute("total").Value)
            };
            es.AddRange(e.Elements("category").Select(Category.FromElement));
            return es;
        }
    }

    public class Category
    {
        public string id;

        public string name;

        public int total_videos;
        public int total_channels;
        public int total_groups;

        public string image;

        public List<Channel> subCategories;

        public static Category FromElement(XElement e)
        {
            return new Category
            {
                id = e.Attribute("word").Value,
                name = e.Element("name").Value,
                //total_videos = int.Parse(e.Element("total_videos").Value),
                //total_channels = int.Parse(e.Element("total_channels").Value),
                //total_groups = int.Parse(e.Element("total_groups").Value),
                //image = "https://f.vimeocdn.com/images_v6/ins_cat_"+e.Attribute("word").Value+".jpg",
                //subCategories = GetSubCategories(e.Element("subcategories"))
            };
        }

        private static List<Channel> GetSubCategories(XElement e)
        {
            return e.Elements("subcategory").ToList().Select(c => new Channel
            {
                //id = c.Attribute("word").Value,
                //name = c.Attribute("name").Value
            }).ToList();
        }
    }
}
