using System.Xml;

namespace MediaBrowser.Plugins.Trailers.Extensions
{
    /// <summary>
    /// Class XmlExtensions
    /// </summary>
    public static class XmlExtensions
    {
        /// <summary>
        /// Reads the string safe.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>System.String.</returns>
        public static string ReadStringSafe(this XmlReader reader)
        {
            var val = reader.ReadElementContentAsString();

            return string.IsNullOrWhiteSpace(val) ? null : val;
        }
    }
}
