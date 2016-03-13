using MediaBrowser.Common.Configuration;
using MetadataViewer.Configuration;
using System.IO;
using System.Reflection;
using System.Text;

namespace MetadataViewer
{
    internal static class InstallHelper
    {
        public static void InstallFiles(IApplicationPaths appPaths, PluginConfiguration config)
        {
            EnsureHtmlFolder(appPaths.PluginsPath);
            CopyResourceToFile("metadataviewer.js", appPaths.PluginsPath);
            CopyResourceToFile("metadataviewer.template.html", appPaths.PluginsPath);
            ModifyMetadataEditor(appPaths);
        }

        private static void ModifyMetadataEditor(IApplicationPaths appPaths)
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

            var htmlPath = GetHtmlPath(appPaths.PluginsPath);
            var targetPath = Path.Combine(htmlPath, "metadataeditor.js");
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            StreamWriter sw = new StreamWriter(targetPath, false);
            sw.Write(sb.ToString());
            sw.Close();
        }

        public static void UninstallFiles(IApplicationPaths appPaths, PluginConfiguration config)
        {
        }

        public static string GetHtmlPath(string pluginsPath)
        {
            return Path.Combine(pluginsPath, "MetadataViewer");
        }

        private static void EnsureHtmlFolder(string pluginsPath)
        {
            var htmlPath = GetHtmlPath(pluginsPath);
            if (!Directory.Exists(htmlPath))
            {
                Directory.CreateDirectory(htmlPath);
            }
        }

        private static void CopyResourceToFile(string resourceName, string pluginsPath)
        {
            var htmlPath = GetHtmlPath(pluginsPath);
            var targetPath = Path.Combine(htmlPath, resourceName);
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            var stream = GetResourceStream(resourceName);
            using (var fileStream = File.Create(targetPath))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            }

            stream.Close();
        }

        private static Stream GetResourceStream(string resourceName)
        {
            var fullQualName = "MetadataViewer.Html." + resourceName;
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(fullQualName);
        }
    }
}
