using System.Text;
using System.Text.Json;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    internal sealed class JsonSnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public JsonSnakeCaseNamingPolicy()
        {
        }

        /// <summary>
        /// Convert a name in camel case to snake case.
        /// </summary>
        /// <param name="name">The name in camel case.</param>
        /// <returns>The name in snake case.</returns>
        public override string ConvertName(string name)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < name.Length; i++)
            {
                var currentChar = name[i];
                if (char.IsUpper(currentChar))
                {
                    if (i > 0)
                    {
                        builder.Append('_');
                    }

                    builder.Append(char.ToLowerInvariant(currentChar));
                }
                else
                {
                    builder.Append(currentChar);
                }
            }

            return builder.ToString();
        }
    }
}
