using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MediaBrowser.Plugins.Vimeo.VimeoAPI.API
{
    public class Subscriptions : List<Subscription>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Subscriptions FromElement(XElement e)
        {
            var ss = new Subscriptions
            {
                on_this_page = int.Parse(e.Attribute("on_this_page").Value),
                page = int.Parse(e.Attribute("page").Value),
                perpage = int.Parse(e.Attribute("perpage").Value),
                total = int.Parse(e.Attribute("total").Value)
            };
            ss.AddRange(e.Elements("subscription").Select(Subscription.FromElement));
            return ss;
        }
    }

    public class Subscription
    {
        public enum SubscriptionTypes
        {
            Likes,
            Appears,
            Channel,
            Uploads,
            Group
        }

        public string subject_id;
        public SubscriptionTypes type;

        public static Subscription FromElement(XElement e)
        {
            var s = new Subscription {subject_id = e.Attribute("subject_id").Value};
            switch (e.Attribute("type").Value)
            {
                case "likes":
                    s.type = SubscriptionTypes.Likes;
                    break;
                case "appears":
                    s.type = SubscriptionTypes.Appears;
                    break;
                case "channel":
                    s.type = SubscriptionTypes.Channel;
                    break;
                case "uploads":
                    s.type = SubscriptionTypes.Uploads;
                    break;
                case "group":
                    s.type = SubscriptionTypes.Group;
                    break;
            }
            return s;
        }
    }
}
