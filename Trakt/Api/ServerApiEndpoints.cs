using System;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using ServiceStack;
using Trakt.Helpers;

namespace Trakt.Api
{
    /// <summary>
    /// 
    /// </summary>
    [Route("/Trakt/Users/{UserId}/Items/{Id}/Rate", "POST")]
    [Api(Description = "Tell the Trakt server to send an item rating to trakt.tv")]
    public class RateItem
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }

        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "Rating", Description = "Rating between 1 - 10 (0 = unrate)", IsRequired = true, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int Rating { get; set; }
        
    }



    /// <summary>
    /// 
    /// </summary>
    [Route("/Trakt/Users/{UserId}/Items/{Id}/Comment", "POST")]
    [Api(Description = "Tell the Trakt server to send an item comment to trakt.tv")]
    public class CommentItem
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }

        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        [ApiMember(Name = "Comment", Description = "Text for the comment", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Comment { get; set; }

        [ApiMember(Name = "Spoiler", Description = "Set to true to indicate the comment contains spoilers. Defaults to false", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Spoiler { get; set; }

        [ApiMember(Name = "Review", Description = "Set to true to indicate the comment is a 200+ word review. Defaults to false", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Review { get; set; }
    }



    /// <summary>
    /// 
    /// </summary>
    [Route("/Trakt/Users/{UserId}/RecommendedMovies", "POST")]
    [Api(Description = "Request a list of recommended Movies based on a users watch history")]
    public class RecommendedMovies
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }

        [ApiMember(Name = "Genre", Description = "Genre slug to filter by. (See http://trakt.tv/api-docs/genres-movies)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public int Genre { get; set; }

        [ApiMember(Name = "StartYear", Description = "4-digit year to filter movies released this year or later", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int StartYear { get; set; }

        [ApiMember(Name = "EndYear", Description = "4-digit year to filter movies released this year or earlier", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int EndYear { get; set; }

        [ApiMember(Name = "HideCollected", Description = "Set true to hide movies in the users collection", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool HideCollected { get; set; }

        [ApiMember(Name = "HideWatchlisted", Description = "Set true to hide movies in the users watchlist", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool HideWatchlisted { get; set; }
    }



    /// <summary>
    /// 
    /// </summary>
    [Route("/Trakt/Users/{UserId}/RecommendedShows", "POST")]
    [Api(Description = "Request a list of recommended Shows based on a users watch history")]
    public class RecommendedShows
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }

        [ApiMember(Name = "Genre", Description = "Genre slug to filter by. (See http://trakt.tv/api-docs/genres-shows)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public int Genre { get; set; }

        [ApiMember(Name = "StartYear", Description = "4-digit year to filter shows released this year or later", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int StartYear { get; set; }

        [ApiMember(Name = "EndYear", Description = "4-digit year to filter shows released this year or earlier", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int EndYear { get; set; }

        [ApiMember(Name = "HideCollected", Description = "Set true to hide shows in the users collection", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool HideCollected { get; set; }

        [ApiMember(Name = "HideWatchlisted", Description = "Set true to hide shows in the users watchlist", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool HideWatchlisted { get; set; }
    }



    /// <summary>
    /// 
    /// </summary>
    public class TraktUriService : IRestfulService
    {
        private readonly TraktApi _traktApi;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraktUriService"/> class.
        /// </summary>
        /// <param name="traktApi">The trakt API.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        public TraktUriService(TraktApi traktApi, ILogger logger, ILibraryManager libraryManager)
        {
            _traktApi = traktApi;
            _logger = logger;
            _libraryManager = libraryManager;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public object Post(RateItem request)
        {
            _logger.Info("RateItem request received");

            var currentItem = _libraryManager.GetItemById(new Guid(request.Id));

            if (currentItem == null)
            {
                _logger.Info("currentItem is null");
                return null;
            }

            return _traktApi.SendItemRating(currentItem, request.Rating, UserHelper.GetTraktUser(request.UserId)).Result;
            
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public object Post(CommentItem request)
        {
            _logger.Info("CommentItem request received");

            var currentItem = _libraryManager.GetItemById(request.Id);

            return _traktApi.SendItemComment(currentItem, request.Comment, request.Spoiler,
                                             UserHelper.GetTraktUser(request.UserId), request.Review).Result;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public object Post(RecommendedMovies request)
        {
            return _traktApi.SendMovieRecommendationsRequest(UserHelper.GetTraktUser(request.UserId)).Result;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public object Post(RecommendedShows request)
        {
            return _traktApi.SendShowRecommendationsRequest(UserHelper.GetTraktUser(request.UserId)).Result;
        }
    }
}
