using System.Xml;

namespace Jellyfin.Plugin.Bookshelf.Extensions
{
    /// <summary>
    ///     Class XmlExtensions.
    /// </summary>
    public static class XmlExtensions
    {
        /// <summary>
        ///     Safes the get string.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        public static string SafeGetString(this XmlDocument doc, string path)
        {
            return SafeGetString(doc, path, null);
        }

        /// <summary>
        ///     Safes the get string.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="path">The path.</param>
        /// <param name="defaultString">The default string.</param>
        /// <returns>System.String.</returns>
        public static string SafeGetString(this XmlDocument doc, string path, string defaultString)
        {
            var rvalNode = doc.SelectSingleNode(path);

            if (rvalNode != null)
            {
                var text = rvalNode.InnerText;

                return !string.IsNullOrWhiteSpace(text) ? text : defaultString;
            }

            return defaultString;
        }
    }
}