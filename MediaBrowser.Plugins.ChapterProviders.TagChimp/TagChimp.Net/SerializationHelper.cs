using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace TagChimp
{
    public static class SerializationHelper
    {
        public static T DeserializeXmlFromFile<T>(string path)
            where T : new()
        {
            if (!File.Exists(path))
                throw new FileNotFoundException();

            string xml = File.ReadAllText(path);
            return DeserializeXml<T>(xml);
        }
        public static T DeserializeXml<T>(string xml)
               where T : new()
        {
            XmlSerializer s = new XmlSerializer(typeof(T));
            using (StringReader sr = new StringReader(xml))
            {
                return (T)s.Deserialize(sr);
            }
        }

        public static string SerializeXml<T>(T item)
            where T : new()
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb, CultureInfo.CurrentCulture))
            {
                ser.Serialize(sw, item);
            }
            return sb.ToString();
        }
    }
}