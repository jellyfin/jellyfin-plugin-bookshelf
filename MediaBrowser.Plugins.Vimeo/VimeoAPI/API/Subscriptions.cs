using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Vimeo.API
{
    public class Subscriptions : List<Subscription>
    {
        public int on_this_page;
        public int page;
        public int perpage;
        public int total;

        public static Subscriptions FromElement(XElement e)
        {
            Subscriptions ss = new Subscriptions();
            ss.on_this_page = int.Parse(e.Attribute("on_this_page").Value);
            ss.page = int.Parse(e.Attribute("page").Value);
            ss.perpage = int.Parse(e.Attribute("perpage").Value);
            ss.total = int.Parse(e.Attribute("total").Value);
            foreach (var s in e.Elements("subscription"))
            {
                ss.Add(Subscription.FromElement(s));
            }
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
            Subscription s = new Subscription();
            s.subject_id = e.Attribute("subject_id").Value;
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
