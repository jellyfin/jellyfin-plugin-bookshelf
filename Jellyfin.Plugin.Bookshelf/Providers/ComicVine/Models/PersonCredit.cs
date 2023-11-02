using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Credit for a person that worked on an issue.
    /// </summary>
    public class PersonCredit
    {
        /// <summary>
        /// Gets the URL pointing to the person detail resource.
        /// </summary>
        public string ApiDetailUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the unique ID of the person.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Gets the name of the person.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the URL pointing to the person on the Comic Vine website.
        /// </summary>
        public string SiteDetailUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the roles for this person (ex: "artist", "writer", etc), separated by commas.
        /// </summary>
        public string Role { get; init; } = string.Empty;

        /// <summary>
        /// Gets the list of roles for this person.
        /// </summary>
        public IEnumerable<PersonCreditRole> Roles => Role.Split(", ").Select(r => Enum.Parse<PersonCreditRole>(r, true));
    }
}
