using System.Xml;

namespace MediaBrowser.Plugins.NextPvr.Helpers
{
    public static class XmlHelper
    {
        public static XmlNode GetSingleNode(string input, string xPath)
        {
            var xml = new XmlDocument();
            xml.LoadXml(input);

            return xml.SelectSingleNode(xPath);
        }

        public static XmlNodeList GetMultipleNodes(string input, string xPath)
        {
            var xml = new XmlDocument();
            xml.LoadXml(input);

            return xml.SelectNodes(xPath);
        }
    }
}
