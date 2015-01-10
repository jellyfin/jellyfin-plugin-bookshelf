using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MediaBrowser.Model.Logging;

namespace GameBrowser.Library.Utils
{
    public static class MameUtils
    {
        private static volatile Dictionary<string, string> _romNamesDictionary;

        private static readonly object LockObject = new object();

        /// <summary>
        /// Get the games full name from the zip file name. Ex: xmcota.zip will return "X-Men: Children of the Atom"
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="logger"></param>
        /// <returns>The games full name</returns>
        public static string GetFullNameFromPath(string path, ILogger logger)
        {
            if (_romNamesDictionary == null)
            {
                lock (LockObject)
                {
                    // Build the dictionary if it's not already populated
                    if (_romNamesDictionary == null)
                    {
                        logger.Info("GameBrowser: Initializing RomNamesDictionary");
                        _romNamesDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        logger.Info("GameBrowser: Building RomNamesDictionary");
                        BuildRomNamesDictionary(logger);
                    }
                }
            }

            var shortName = Path.GetFileNameWithoutExtension(path);

            if (shortName != null)
            {
                string value;

                if (_romNamesDictionary.TryGetValue(shortName, out value))
                {
                    return value;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Determine if the file is actually a BIOS rom
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>true if Bios</returns>
        public static bool IsBiosRom(string path)
        {

            return false;
        }

        private static void BuildRomNamesDictionary(ILogger logger)
        {

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GameBrowser.Resources.mame_game_list.txt"))
            {
                if (stream == null)
                {
                    //logger.Info("GameBrowser: Stream is null");
                    return;
                }
                
                using (var reader = new StreamReader(stream))
                {

                    while (reader.Peek() >= 0)
                    {
                        //logger.Info("GameBrowser: Readline");
                        var line = reader.ReadLine();

                        if (line == null)
                        {
                            //logger.Info("GameBrowser: line == null");
                            continue;
                        }

                        var index = line.IndexOf(" ", StringComparison.Ordinal);

                        if (index == -1) continue;

                        var key = line.Substring(0, index);
                        //logger.Info("GameBrowser: key = '" + key + "'");

                        // Trim whitespace, quotes, then whitespace again
                        var value = line.Substring(index).Trim().Trim('"').Trim();

                        _romNamesDictionary.Add(key, value);
                    }
                }
            }
        }
    }
}
