using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Comic Vine API response.
    /// </summary>
    /// <typeparam name="T">Type of object returned by the response.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Gets an integer indicating the result of the request. Acceptable values are:
        /// <list type="bullet">
        ///     <item>
        ///         <term>1</term>
        ///         <description>OK</description>
        ///     </item>
        ///    <item>
        ///        <term>100</term>
        ///        <description>Invalid API Key</description>
        ///    </item>
        ///    <item>
        ///        <term>101</term>
        ///        <description>Object Not Found</description>
        ///    </item>
        ///    <item>
        ///        <term>102</term>
        ///        <description>Error in URL Format</description>
        ///    </item>
        ///    <item>
        ///        <term>103</term>
        ///        <description>'jsonp' format requires a 'json_callback' argument</description>
        ///    </item>
        ///    <item>
        ///        <term>104</term>
        ///        <description>Filter Error</description>
        ///    </item>
        ///    <item>
        ///        <term>105</term>
        ///        <description>Subscriber only video is for subscribers only</description>
        ///    </item>
        /// </list>
        /// </summary>
        public int StatusCode { get; init; }

        /// <summary>
        /// Gets a text string representing the StatusCode.
        /// </summary>
        public string Error { get; init; } = string.Empty;

        /// <summary>
        /// Gets the number of total results matching the filter conditions specified.
        /// </summary>
        public int NumberOfTotalResults { get; init; }

        /// <summary>
        /// Gets the number of results on this page.
        /// </summary>
        public int NumberOfPageResults { get; init; }

        /// <summary>
        /// Gets the value of the limit filter specified, or 10 if not specified.
        /// </summary>
        public int Limit { get; init; }

        /// <summary>
        /// Gets the value of the offset filter specified, or 0 if not specified.
        /// </summary>
        public int Offset { get; init; }

        /// <summary>
        /// Gets zero or more items that match the filters specified.
        /// </summary>
        public IEnumerable<T> Results { get; init; } = Enumerable.Empty<T>();

        /// <summary>
        /// Gets a value indicating whether the response is an error.
        /// </summary>
        public bool IsError => StatusCode != 1;
    }
}
