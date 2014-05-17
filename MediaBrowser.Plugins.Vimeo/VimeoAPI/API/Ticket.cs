using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Vimeo.API
{
    [Serializable]
    public class Ticket
    {
        public string id;
        public string endpoint;
        public string host;
        public string max_file_size;

        public Ticket() { }
        public Ticket(string id, string endpoint) { this.id = id; this.endpoint = endpoint; }

        public static Ticket FromElement(XElement e)
        {
            return new Ticket
            {
                id = e.Attribute("id").Value,
                endpoint = e.Attribute("endpoint").Value,
                host = e.Attribute("host").Value,
                max_file_size = e.Attribute("max_file_size").Value
            };
        }
    }

    public class Chunks
    {
        public class Chunk
        {
            public int id;
            public long size;

            public static Chunk FromElement(XElement e)
            {
                return new Chunk
                {
                    id = int.Parse(e.Attribute("id").Value),
                    size = long.Parse(e.Attribute("size").Value)
                };
            }
        }
        
        public string ticket_id;
        public List<Chunk> Items;

        public static Chunks FromElement(XElement e)
        {
            var r = new Chunks
            {
                ticket_id = e.Attribute("id").Value,
                Items = new List<Chunk>()
            };
            foreach (var item in e.Element("chunks").Elements("chunk"))
            {
                r.Items.Add(Chunk.FromElement(item));
            }
            return r;
        }
    }
}
