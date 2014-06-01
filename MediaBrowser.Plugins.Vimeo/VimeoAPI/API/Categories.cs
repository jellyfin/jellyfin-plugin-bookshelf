using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using MediaBrowser.Model.Logging;

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
            if (e.Attribute("on_this_page") != null)
            {
                var es = new Categories
                {
                    on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                    page = int.Parse(e.Attribute("page").Value),
                    perpage = int.Parse(e.Attribute("perpage").Value),
                    total = int.Parse(e.Attribute("total").Value)
                };
                es.AddRange(e.Elements("category").Select(Category.FromElement).Where(item => item.subCategories.Any()));

                return es;
            }

            return null;
        }
    }

    public class Category
    {
        public string id;

        public string name;

        public int total_videos;
        public int total_channels;
        public int total_groups;
        public bool subCat = false;
        public string image;

        public List<Channel> subCategories;

        public static Category FromElement(XElement e)
        {
            var cat = new Category
            {
                id = e.Attribute("word").Value,
                name = e.Element("name").Value,
                subCategories = GetSubCategories(e.Element("subcategories")),
            };

            /*if (e.Element("total_videos") != null)
                cat.total_videos = int.Parse(e.Element("total_videos").Value);
            if (e.Element("total_channels") != null)
                cat.total_channels = int.Parse(e.Element("total_channels").Value);
            if (e.Element("total_groups") != null)
                cat.total_groups = int.Parse(e.Element("total_groups").Value);
            */
            cat.image = "https://f.vimeocdn.com/images_v6/ins_cat_" + cat.id + ".jpg";
            return cat;
        }

        private static List<Channel> GetSubCategories(XElement e)
        {
            var subList = new List<Channel>();

            try
            {
                var xElement = e.Element("subcategory");
                if (xElement != null)
                {
                    subList.AddRange(e.Elements("subcategory").Select(item => new Channel
                    {
                        id = item.Attribute("word").Value,
                        name = item.Attribute("name").Value
                    }));
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine("ERROR! " + ex);
            }

            return subList;
        }
    }
}
