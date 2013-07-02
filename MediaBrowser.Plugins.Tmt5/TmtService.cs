using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Tmt5
{
    public class TmtService
    {
        /// <summary>
        /// Gets the play state directory.
        /// </summary>
        /// <value>The play state directory.</value>
        public static string PlayStateDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ArcSoft");
            }
        }

        /// <summary>
        /// Parses the ini file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>NameValueCollection.</returns>
        public static NameValueCollection ParseIniFile(string path)
        {
            var values = new NameValueCollection();

            foreach (var line in File.ReadAllLines(path))
            {
                var data = line.Split('=');

                if (data.Length < 2) continue;

                var key = data[0];

                var value = data.Length == 2 ? data[1] : string.Join(string.Empty, data, 1, data.Length - 1);

                values[key] = value;
            }

            return values;
        }
    }
}
