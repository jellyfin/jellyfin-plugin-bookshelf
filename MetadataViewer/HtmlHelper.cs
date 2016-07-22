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

        public static MemoryStream ModifiedContextMenu { get; private set; }


        public static void InstallFiles(IApplicationPaths appPaths, PluginConfiguration config)
        {
            ViewerScript = GetResourceStream("metadataviewer.js");
            ViewerTemplate = GetResourceStream("metadataviewer.template.html");
            ModifiedContextMenu = ModifyContextMenu(appPaths);
        }

        private static MemoryStream ModifyContextMenu(IApplicationPaths appPaths)
        {
            var sb = new StringBuilder();

            var editorPath = Path.Combine(appPaths.ProgramSystemPath, "dashboard-ui", "bower_components", "emby-webcomponents", "itemcontextmenu.js");

            var lines = File.ReadLines(editorPath);

            bool mark1Hit = false;

            foreach (var line in lines)
            {
                if (line.Contains("case 'identify':"))
                {
                    sb.AppendLine("                case 'showmetadata':");
                    sb.AppendLine("                    {");
                    sb.AppendLine("                        require(['components/metadataviewer/metadataviewer'], function (metadataviewer) {");
                    sb.AppendLine("                            metadataviewer.show(itemId).then(getResolveFunction(resolve, id, true), getResolveFunction(resolve, id));");
                    sb.AppendLine("                            });");
                    sb.AppendLine("                            break;");
                    sb.AppendLine("                    }");
                }

                sb.AppendLine(line);

                if (line.Contains("id: 'identify'"))
                {
                    mark1Hit = true;
                }

                if (mark1Hit && string.IsNullOrWhiteSpace(line))
                {
                    mark1Hit = false;
                    sb.AppendLine();
                    sb.AppendLine("            if (!isTheater && options.identify !== false) {");
                    sb.AppendLine("                if (itemHelper.canIdentify(user, item.Type)) {");
                    sb.AppendLine("                    commands.push({");
                    sb.AppendLine("                        name: 'View Raw Metadata',");
                    sb.AppendLine("                        id: 'showmetadata'");
                    sb.AppendLine("                    });");
                    sb.AppendLine("                }");
                    sb.AppendLine("            }");
                    sb.AppendLine("");
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
            ModifiedContextMenu = null;
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
