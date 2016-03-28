using MediaBrowser.Common.Configuration;
using MetadataViewer.Configuration;
using System.IO;
using System.Reflection;
using System.Text;

namespace MetadataViewer
{
    internal static class HtmlHelper
    {
        public static MemoryStream ViewerScript { get; private set; }

        public static MemoryStream ViewerTemplate { get; private set; }

        public static MemoryStream ModifiedEditor { get; private set; }


        public static void InstallFiles(IApplicationPaths appPaths, PluginConfiguration config)
        {
            ViewerScript = GetResourceStream("metadataviewer.js");
            ViewerTemplate = GetResourceStream("metadataviewer.template.html");
            ModifiedEditor = ModifyMetadataEditor(appPaths);
        }

        private static MemoryStream ModifyMetadataEditor(IApplicationPaths appPaths)
        {
            var sb = new StringBuilder();

            var editorPath = Path.Combine(appPaths.ProgramSystemPath, "dashboard-ui\\components\\metadataeditor\\metadataeditor.js");

            var lines = File.ReadLines(editorPath);

            bool mark1Hit = false;
            bool mark2Hit = false;

            foreach (var line in lines)
            {
                sb.AppendLine(line);

                if (line.Contains("id: 'identify',"))
                {
                    mark1Hit = true;
                }

                if (mark1Hit && string.IsNullOrWhiteSpace(line))
                {
                    mark1Hit = false;
                    sb.AppendLine();
                    sb.AppendLine("        items.push({");
                    sb.AppendLine("            name: 'View Raw Metadata',");
                    sb.AppendLine("            id: 'showmetadata',");
                    sb.AppendLine("            ironIcon: 'info'");
                    sb.AppendLine("        });");
                    sb.AppendLine("");
                }

                if (line.Contains("LibraryBrowser.identifyItem(currentItem.Id);"))
                {
                    mark2Hit = true;
                }

                if (mark2Hit && line.Contains("break;"))
                {
                    mark2Hit = false;
                    sb.AppendLine();
                    sb.AppendLine("                        case 'showmetadata':");
                    sb.AppendLine("                            require(['components/metadataviewer/metadataviewer'], function (metadataviewer) {");
                    sb.AppendLine("                                metadataviewer.show(currentItem.Id);");
                    sb.AppendLine("                            });");
                    sb.AppendLine("                            break;");
                }
            }

            var memStream = new MemoryStream();

            using (var sw = new StreamWriter(memStream, Encoding.UTF8, sb.Length, true))
            {
                sw.Write(sb.ToString());
            }

            return memStream;
        }

        public static void UninstallFiles(IApplicationPaths appPaths, PluginConfiguration config)
        {
            ViewerScript = null;
            ViewerTemplate = null;
            ModifiedEditor = null;
        }

        public static string GetHtmlPath(string pluginsPath)
        {
            return Path.Combine(pluginsPath, "MetadataViewer");
        }

        private static MemoryStream GetResourceStream(string resourceName)
        {
            var fullQualName = "MetadataViewer.Html." + resourceName;
            var stream = typeof(HtmlHelper).GetTypeInfo().Assembly.GetManifestResourceStream(fullQualName);
            var memStream = new MemoryStream((int)stream.Length);
            stream.CopyTo(memStream);
            return memStream;
        }
    }
}
