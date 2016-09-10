using MediaBrowser.Common.Configuration;
using MovieOrganizer.Configuration;
using System.IO;
using System.Reflection;
using System.Text;

namespace MovieOrganizer
{
    internal static class HtmlHelper
    {
        public static MemoryStream OrganizerScript { get; private set; }

        public static MemoryStream OrganizerTemplate { get; private set; }



        public static void InstallFiles(IApplicationPaths appPaths, PluginConfiguration config)
        {
            OrganizerScript = GetResourceStream("fileorganizer.js");
            OrganizerTemplate = GetResourceStream("fileorganizer.template.html");
        }

        public static void UninstallFiles(IApplicationPaths appPaths, PluginConfiguration config)
        {
            OrganizerScript = null;
            OrganizerTemplate = null;
        }

        private static MemoryStream GetResourceStream(string resourceName)
        {
            var fullQualName = "MovieOrganizer.Html." + resourceName;
            var stream = typeof(HtmlHelper).GetTypeInfo().Assembly.GetManifestResourceStream(fullQualName);
            var memStream = new MemoryStream((int)stream.Length);
            stream.CopyTo(memStream);
            return memStream;
        }
    }
}
