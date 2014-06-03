using System;

namespace MediaBrowser.Plugins.ArgusTV.Helpers
{
    public static class GeneralHelpers
    {
        public static bool ContainsWord(string source, string value, StringComparison comparisonType)
        {
            return source.IndexOf(value, comparisonType) >= 0;
        }
    }
}
