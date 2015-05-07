using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EmbyTV.TunerHost;
using MediaBrowser.Model.Serialization;

namespace EmbyTV.GeneralHelpers
{
    public static class Helpers
    {
        public static IEnumerable<Type> GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal) && typeof(ITunerHost).IsAssignableFrom(t) && t.IsPublic);
        }

        public static void  CreateFileCopy(object obj, string filePath,IJsonSerializer serializer)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            if (obj != null)
            {
               serializer.SerializeToFile(obj, filePath);
            }
        }
        public static void CreateFileCopy(object obj, string filePath, IXmlSerializer serializer)
        {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                if (obj != null)
                {
                    serializer.SerializeToFile(obj, filePath);
                }
        }
        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
