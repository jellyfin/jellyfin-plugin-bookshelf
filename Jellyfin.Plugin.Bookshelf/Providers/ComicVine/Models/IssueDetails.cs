using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Details of an issue.
    /// </summary>
    public class IssueDetails : IssueSearch
    {
        /// <summary>
        /// Gets the list of people who worked on this issue.
        /// </summary>
        public IReadOnlyList<PersonCredit> PersonCredits { get; init; } = Array.Empty<PersonCredit>();
    }
}
