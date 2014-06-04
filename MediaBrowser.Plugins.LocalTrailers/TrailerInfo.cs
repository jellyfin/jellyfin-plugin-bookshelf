using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Plugins.LocalTrailers
{
    /// <summary>
    /// This is a stub class used to hold information about a trailer
    /// </summary>
    public class TrailerInfo
    {
        public string Name { get; set; }
        public string OfficialRating { get; set; }
        public string Overview { get; set; }
        public float? CommunityRating { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Studios { get; set; }
        public List<PersonInfo> People { get; set; }

        /// <summary>
        /// Gets or sets the image URL.
        /// </summary>
        /// <value>The image URL.</value>
        public string ImageUrl { get; set; }
        /// <summary>
        /// Gets or sets the hd image URL.
        /// </summary>
        /// <value>The hd image URL.</value>
        public string HdImageUrl { get; set; }
        /// <summary>
        /// Gets or sets the trailer URL.
        /// </summary>
        /// <value>The trailer URL.</value>
        public string TrailerUrl { get; set; }
        /// <summary>
        /// Gets or sets the post date.
        /// </summary>
        /// <value>The post date.</value>
        public DateTime PostDate { get; set; }
        public DateTime? PremiereDate { get; set; }
        public int? ProductionYear { get; set; }

        public long? RunTimeTicks { get; set; }

        public TrailerInfo()
        {
            Studios = new List<string>();
            Genres = new List<string>();
            People = new List<PersonInfo>();
        }
    }
}
