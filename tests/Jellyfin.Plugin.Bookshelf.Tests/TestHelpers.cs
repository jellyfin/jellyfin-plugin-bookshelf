namespace Jellyfin.Plugin.Bookshelf.Tests
{
    internal static class TestHelpers
    {
        /// <summary>
        /// Get the full path to a fixture file.
        /// </summary>
        /// <param name="fileName">Name of the fixture file.</param>
        /// <returns>The full path to the fixture file.</returns>
        public static string GetFixturePath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", fileName);
        }

        /// <summary>
        /// Get the content of a fixture file.
        /// </summary>
        /// <param name="fileName">Name of the fixture file.</param>
        /// <returns>The file's content.</returns>
        /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
        public static string GetFixture(string fileName)
        {
            var filePath = GetFixturePath(fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The fixture file '{filePath}' was not found.");
            }

            return File.ReadAllText(filePath);
        }
    }
}
