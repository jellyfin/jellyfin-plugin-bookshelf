using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using Trakt.Api.DataContracts;
using Trakt.Helpers;
using Trakt.Model;
using MediaBrowser.Model.Entities;

namespace Trakt.Api
{
    /// <summary>
    /// 
    /// </summary>
    public class TraktApi
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;

        public TraktApi(IJsonSerializer jsonSerializer, ILogger logger, IHttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<TraktResponseDataContract> AccountTest(TraktUser traktUser)
        {
            var data = new Dictionary<string, string> {{"username", traktUser.UserName}, {"password", traktUser.PasswordHash}};

            var response =
                await
                _httpClient.Post(TraktUris.AccountTest, data, Plugin.Instance.TraktResourcePool,
                                                                     CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response);
        }



        /// <summary>
        /// Return information about the user, including ratings format
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<AccountSettingsDataContract> GetUserAccount(TraktUser traktUser)
        {
            var data = new Dictionary<string, string> { { "username", traktUser.UserName }, { "password", traktUser.PasswordHash } };

            var response =
                await
                _httpClient.Post(TraktUris.AccountSettings, data, Plugin.Instance.TraktResourcePool,
                                                                     CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<AccountSettingsDataContract>(response);
        }



        /// <summary>
        /// Return a list of the users friends
        /// </summary>
        /// <param name="traktUser">The user who's friends you want to retrieve</param>
        /// <returns>A TraktFriendDataContract</returns>
        public async Task<TraktFriendDataContract> GetUserFriends(TraktUser traktUser)
        {
            var data = new Dictionary<string, string> { { "username", traktUser.UserName }, { "password", traktUser.PasswordHash } };

            var response = await _httpClient.Post(string.Format(TraktUris.Friends, traktUser.UserName), data, Plugin.Instance.TraktResourcePool,
                                                                     CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktFriendDataContract>(response);
            
        }



        /// <summary>
        /// Report to trakt.tv that a movie is being watched, or has been watched.
        /// </summary>
        /// <param name="movie">The movie being watched/scrobbled</param>
        /// <param name="mediaStatus">MediaStatus enum dictating whether item is being watched or scrobbled</param>
        /// <param name="traktUser">The user that watching the current movie</param>
        /// <returns>A standard TraktResponse Data Contract</returns>
        public async Task<TraktResponseDataContract> SendMovieStatusUpdateAsync(Movie movie, MediaStatus mediaStatus, TraktUser traktUser)
        {
            var data = new Dictionary<string,string>
                           {
                               {"username", traktUser.UserName},
                               {"password", traktUser.PasswordHash},
                               {"imdb_id", movie.GetProviderId(MetadataProviders.Imdb)}
                           };

            if (movie.ProviderIds != null && movie.ProviderIds.ContainsKey("Tmdb"))
            {
                data.Add("tmdb_id", movie.ProviderIds["Tmdb"]);
            }

            data.Add("title", movie.Name);
            data.Add("year", movie.ProductionYear != null ? movie.ProductionYear.ToString() : "");
            data.Add("duration", movie.RunTimeTicks != null ? ((int)((movie.RunTimeTicks / 10000000) / 60)).ToString(CultureInfo.InvariantCulture) : "");


            Stream response = null;

            if (mediaStatus == MediaStatus.Watching)
                response = await _httpClient.Post(TraktUris.MovieWatching, data, Plugin.Instance.TraktResourcePool, CancellationToken.None).ConfigureAwait(false);
            else if (mediaStatus == MediaStatus.Scrobble)
                response = await _httpClient.Post(TraktUris.MovieScrobble, data, Plugin.Instance.TraktResourcePool, CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response);
        }



        /// <summary>
        /// Reports to trakt.tv that an episode is being watched. Or that Episode(s) have been watched.
        /// </summary>
        /// <param name="episode">The episode being watched</param>
        /// <param name="status">Enum indicating whether an episode is being watched or scrobbled</param>
        /// <param name="traktUser">The user that's watching the episode</param>
        /// <returns>A List of standard TraktResponse Data Contracts</returns>
        public async Task<List<TraktResponseDataContract>> SendEpisodeStatusUpdateAsync(Episode episode, MediaStatus status, TraktUser traktUser)
        {
            var responses = new List<TraktResponseDataContract>();

            
            // We're Scrobbling a multi-episode file
            if (episode.IndexNumberEnd != null && episode.IndexNumberEnd != episode.IndexNumber && status.Equals(MediaStatus.Scrobble))
            {
                for (var i = episode.IndexNumber; i <= episode.IndexNumberEnd; i++)
                {
                    var response = await SendEpisodeStatusUpdateInternalAsync(episode, i, status, traktUser);
                    responses.Add(response);
                }
            }
            // It's a single-episode file, or we're just sending the watching status of a multi-episode file. 
            else
            {
                var response = await SendEpisodeStatusUpdateInternalAsync(episode, episode.IndexNumber, status, traktUser);
                responses.Add(response);
            }

            return responses;
        }

        private async Task<TraktResponseDataContract> SendEpisodeStatusUpdateInternalAsync(Episode episode, int? index, MediaStatus status, TraktUser traktUser)
        {
            var data = new Dictionary<string, string>
                           {
                               {"username", traktUser.UserName},
                               {"password", traktUser.PasswordHash}
                           };

            if (episode.Series.ProviderIds != null)
            {
                if (episode.Series.ProviderIds.ContainsKey("Imdb"))
                    data.Add("imdb_id", episode.Series.ProviderIds["Imdb"]);

                if (episode.Series.ProviderIds.ContainsKey("Tvdb"))
                    data.Add("tvdb_id", episode.Series.ProviderIds["Tvdb"]);
            }

            if (episode.Series == null || episode.AiredSeasonNumber == null)
                return null;

            data.Add("title", episode.Series.Name);
            data.Add("year", episode.Series.ProductionYear != null ? episode.Series.ProductionYear.ToString() : "");
            data.Add("season", episode.AiredSeasonNumber != null ? episode.AiredSeasonNumber.ToString() : "");
            data.Add("episode", index != null ? index.ToString() : "");
            data.Add("duration", episode.RunTimeTicks != null ? ((int)((episode.RunTimeTicks / 10000000) / 60)).ToString(CultureInfo.InvariantCulture) : "");


            Stream response = null;

            if (status == MediaStatus.Watching)
                response = await _httpClient.Post(TraktUris.ShowWatching, data, Plugin.Instance.TraktResourcePool, CancellationToken.None).ConfigureAwait(false);
            else if (status == MediaStatus.Scrobble)
                response = await _httpClient.Post(TraktUris.ShowScrobble, data, Plugin.Instance.TraktResourcePool, CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response);
        }


        /// <summary>
        /// Add or remove a list of movies to/from the users trakt.tv library
        /// </summary>
        /// <param name="movies">The movies to add</param>
        /// <param name="traktUser">The user who's library is being updated</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="eventType"></param>
        /// <returns>Task{TraktResponseDataContract}.</returns>
        public async Task<TraktResponseDataContract> SendLibraryUpdateAsync(List<Movie> movies, TraktUser traktUser, CancellationToken cancellationToken, EventType eventType)
        {
            if (movies == null)
                throw new ArgumentNullException("movies");
            if (traktUser == null)
                throw new ArgumentNullException("traktUser");

            if (eventType == EventType.Update) return null;

            var moviesPayload = movies.Select(m => new
                                                       {
                                                           title = m.Name, imdb_id = m.GetProviderId(MetadataProviders.Imdb), year = m.ProductionYear ?? 0
                                                       }).Cast<object>().ToList();

            var data = new
                           {
                               username = traktUser.UserName,
                               password = traktUser.PasswordHash,
                               movies = moviesPayload
                           };
            
            var options = new HttpRequestOptions
                              {
                                  RequestContent = _jsonSerializer.SerializeToString(data),
                                  ResourcePool = Plugin.Instance.TraktResourcePool,
                                  CancellationToken = cancellationToken
                              };

            switch (eventType)
            {
                case EventType.Add:
                    options.Url = TraktUris.MovieLibrary;
                    break;
                case EventType.Remove:
                    options.Url = TraktUris.MovieUnLibrary;
                    break;
            }

            var response = await _httpClient.Post(options).ConfigureAwait(false);
            
            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response.Content);
        }


        /// <summary>
        /// Add or remove a list of Episodes to/from the users trakt.tv library
        /// </summary>
        /// <param name="episodes">The episodes to add</param>
        /// <param name="traktUser">The user who's library is being updated</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="eventType"></param>
        /// <returns>Task{TraktResponseDataContract}.</returns>
        public async Task<TraktResponseDataContract> SendLibraryUpdateAsync(IReadOnlyList<Episode> episodes, TraktUser traktUser, CancellationToken cancellationToken, EventType eventType)
        {
            if (episodes == null)
                throw new ArgumentNullException("episodes");

            if (traktUser == null)
                throw new ArgumentNullException("traktUser");

            if (eventType == EventType.Update) return null;

            var episodesPayload = new List<Object>();

            foreach (var episode in episodes)
            {
                // It's a multi-episode file. Add all episodes
                if (episode.IndexNumberEnd != null && episode.IndexNumberEnd != episode.IndexNumber)
                {
                    for (var i = episode.IndexNumber; i <= episode.IndexNumberEnd; i++)
                    {
                        episodesPayload.Add(new { season = episode.ParentIndexNumber, episode = i });
                    }
                }
                // it's a single-episode file
                else
                {
                    episodesPayload.Add(new { season = episode.ParentIndexNumber, episode = episode.IndexNumber });
                }
            }

            var data = new
                           {
                               username = traktUser.UserName,
                               password = traktUser.PasswordHash,
                               imdb_id = episodes[0].Series.GetProviderId(MetadataProviders.Imdb),
                               tvdb_id = episodes[0].Series.GetProviderId(MetadataProviders.Tvdb),
                               title = episodes[0].Series.Name,
                               year = (episodes[0].Series.ProductionYear ?? 0).ToString(CultureInfo.InvariantCulture),
                               episodes = episodesPayload
                           };

            var options = new HttpRequestOptions
            {
                RequestContent = _jsonSerializer.SerializeToString(data),
                ResourcePool = Plugin.Instance.TraktResourcePool,
                CancellationToken = cancellationToken
            };
            
            

            switch (eventType)
            {
                case EventType.Add:
                    options.Url = TraktUris.ShowEpisodeLibrary;
                    break;
                case EventType.Remove:
                    options.Url = TraktUris.ShowEpisodeUnLibrary;
                    break;
            }

            var response = await _httpClient.Post(options).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response.Content);
        }



        /// <summary>
        /// Add or remove a Show(Series) to/from the users trakt.tv library
        /// </summary>
        /// <param name="show">The show to remove</param>
        /// <param name="traktUser">The user who's library is being updated</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="eventType"></param>
        /// <returns>Task{TraktResponseDataContract}.</returns>
        public async Task<TraktResponseDataContract> SendLibraryUpdateAsync(Series show, TraktUser traktUser, CancellationToken cancellationToken, EventType eventType)
        {
            if (show == null)
                throw new ArgumentNullException("show");
            if (traktUser == null)
                throw new ArgumentNullException("traktUser");

            if (eventType == EventType.Update) return null;

            var data = new
            {
                username = traktUser.UserName,
                password = traktUser.PasswordHash,
                tvdb_id = show.GetProviderId(MetadataProviders.Tvdb),
                title = show.Name,
                year = show.ProductionYear
            };

            var options = new HttpRequestOptions
                                                {
                                                    RequestContent = _jsonSerializer.SerializeToString(data),
                                                    ResourcePool = Plugin.Instance.TraktResourcePool,
                                                    CancellationToken = cancellationToken
                                                };

            switch (eventType)
            {
                case EventType.Add:
                    
                    break;
                case EventType.Remove:
                    options.Url = TraktUris.ShowUnLibrary;
                    break;
            }

            var response = await _httpClient.Post(options).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response.Content);
        }



        /// <summary>
        /// Rate an item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="rating"></param>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<TraktResponseDataContract> SendItemRating(BaseItem item, int rating, TraktUser traktUser)
        {
            string url;
            var data = new Dictionary<string, string>
                           {
                               {"username", traktUser.UserName},
                               {"password", traktUser.PasswordHash}
                           };

            if (item is Movie)
            {
                if (item.ProviderIds != null && item.ProviderIds.ContainsKey("Imdb"))
                    data.Add("imdb_id", item.ProviderIds["Imdb"]);
                
                data.Add("title", item.Name);
                data.Add("year", item.ProductionYear != null ? item.ProductionYear.ToString() : "");
                url = TraktUris.RateMovie;
            }
            else
            {
                var episode = item as Episode;
                if (episode != null)
                {
                    data.Add("title", episode.Series.Name);
                    data.Add("year", episode.Series.ProductionYear != null ? episode.Series.ProductionYear.ToString() : "");
                    
                    if (episode.Series.ProviderIds != null)
                    {
                        if (episode.Series.ProviderIds.ContainsKey("Imdb"))
                            data.Add("imdb_id", episode.Series.ProviderIds["Imdb"]);

                        if (episode.Series.ProviderIds.ContainsKey("Tvdb"))
                            data.Add("tvdb_id", episode.Series.ProviderIds["Tvdb"]);
                    }

                    data.Add("season", episode.AiredSeasonNumber.ToString());
                    data.Add("episode", episode.IndexNumber.ToString());
                    url = TraktUris.RateEpisode;
                }
                else // It's a Series
                {
                    data.Add("title", item.Name);
                    data.Add("year", item.ProductionYear != null ? item.ProductionYear.ToString() : "");
                    
                    if (item.ProviderIds != null)
                    {
                        if (item.ProviderIds.ContainsKey("Imdb"))
                            data.Add("imdb_id", item.ProviderIds["Imdb"]);

                        if (item.ProviderIds.ContainsKey("Tvdb"))
                            data.Add("tvdb_id", item.ProviderIds["Tvdb"]);
                    }

                    url = TraktUris.RateShow;
                }
            }

            data.Add("rating", rating.ToString(CultureInfo.InvariantCulture));
            
            var response =
                await
                _httpClient.Post(url, data, Plugin.Instance.TraktResourcePool,
                                                 CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="comment"></param>
        /// <param name="containsSpoilers"></param>
        /// <param name="traktUser"></param>
        /// <param name="isReview"></param>
        /// <returns></returns>
        public async Task<TraktResponseDataContract> SendItemComment(BaseItem item, string comment, bool containsSpoilers, TraktUser traktUser, bool isReview = false)
        {
            string url;
            var data = new Dictionary<string, string>
                           {
                               {"username", traktUser.UserName},
                               {"password", traktUser.PasswordHash}
                           };

            if (item is Movie)
            {
                if (item.ProviderIds != null && item.ProviderIds.ContainsKey("Imdb"))
                    data.Add("imdb_id", item.ProviderIds["Imdb"]);
                
                data.Add("title", item.Name);
                data.Add("year", item.ProductionYear != null ? item.ProductionYear.ToString() : "");
                url = TraktUris.CommentMovie;
            }
            else
            {
                var episode = item as Episode;
                if (episode != null)
                {
                    if (episode.Series.ProviderIds != null)
                    {
                        if (episode.Series.ProviderIds.ContainsKey("Imdb"))
                            data.Add("imdb_id", episode.Series.ProviderIds["Imdb"]);

                        if (episode.Series.ProviderIds.ContainsKey("Tvdb"))
                            data.Add("tvdb_id", episode.Series.ProviderIds["Tvdb"]);
                    }

                    data.Add("season", episode.AiredSeasonNumber.ToString());
                    data.Add("episode", episode.IndexNumber.ToString());
                    url = TraktUris.CommentEpisode;   
                }
                else // It's a Series
                {
                    data.Add("title", item.Name);
                    data.Add("year", item.ProductionYear != null ? item.ProductionYear.ToString() : "");

                    if (item.ProviderIds != null)
                    {
                        if (item.ProviderIds.ContainsKey("Imdb"))
                            data.Add("imdb_id", item.ProviderIds["Imdb"]);

                        if (item.ProviderIds.ContainsKey("Tvdb"))
                            data.Add("tvdb_id", item.ProviderIds["Tvdb"]);
                    }
                    
                    url = TraktUris.CommentShow;
                }
            }

            data.Add("comment", comment);
            data.Add("spoiler", containsSpoilers.ToString());
            data.Add("review", isReview.ToString());

            Stream response =
                await
                _httpClient.Post(url, data, Plugin.Instance.TraktResourcePool,
                                                 CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<TraktMovieDataContract>> SendMovieRecommendationsRequest(TraktUser traktUser)
        {
            var data = new Dictionary<string, string>
                           {{"username", traktUser.UserName}, {"password", traktUser.PasswordHash}};

            Stream response =
                await
                _httpClient.Post(TraktUris.RecommendationsMovies, data, Plugin.Instance.TraktResourcePool,
                                                 CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<List<TraktMovieDataContract>>(response);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<TraktShowDataContract>> SendShowRecommendationsRequest(TraktUser traktUser)
        {
            var data = new Dictionary<string, string> { { "username", traktUser.UserName }, { "password", traktUser.PasswordHash } };

            Stream response =
                await
                _httpClient.Post(TraktUris.RecommendationsShows, data, Plugin.Instance.TraktResourcePool,
                                                 CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<List<TraktShowDataContract>>(response);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<TraktMovieDataContract>>  SendGetAllMoviesRequest(TraktUser traktUser)
        {
            var data = new Dictionary<string, string> { { "username", traktUser.UserName }, { "password", traktUser.PasswordHash } };

            var response =
                await
                _httpClient.Post(string.Format(TraktUris.MoviesAll, traktUser.UserName), data, Plugin.Instance.TraktResourcePool,
                                                 CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<List<TraktMovieDataContract>>(response);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<TraktUserLibraryShowDataContract>> SendGetCollectionShowsRequest(TraktUser traktUser)
        {
            var data = new Dictionary<string, string> { { "username", traktUser.UserName }, { "password", traktUser.PasswordHash } };

            var response =
                await
                _httpClient.Post(string.Format(TraktUris.ShowsCollection, traktUser.UserName), data, Plugin.Instance.TraktResourcePool,
                                                 CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<List<TraktUserLibraryShowDataContract>>(response);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<TraktUserLibraryShowDataContract>> SendGetWatchedShowsRequest(TraktUser traktUser)
        {
            var data = new Dictionary<string, string> { { "username", traktUser.UserName }, { "password", traktUser.PasswordHash } };

            var response =
                await
                _httpClient.Post(string.Format(TraktUris.ShowsWatched, traktUser.UserName), data, Plugin.Instance.TraktResourcePool,
                                                 CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<List<TraktUserLibraryShowDataContract>>(response);
        }



        /// <summary>
        /// Send a list of movies to trakt.tv that have been marked watched or unwatched
        /// </summary>
        /// <param name="movies">The list of movies to send</param>
        /// <param name="traktUser">The trakt user profile that is being updated</param>
        /// <param name="seen">True if movies are being marked seen, false otherwise</param>
        /// <param name="cancellationToken">The Cancellation Token</param>
        /// <returns></returns>
        public async Task<TraktResponseDataContract> SendMoviePlaystateUpdates(List<Movie> movies, TraktUser traktUser, bool seen, CancellationToken cancellationToken)
        {
            if (movies == null)
                throw new ArgumentNullException("movies");
            if (traktUser == null)
                throw new ArgumentNullException("traktUser");

            var moviesPayload = movies.Select(m => new
            {
                title = m.Name,
                imdb_id = m.GetProviderId(MetadataProviders.Imdb),
                year = m.ProductionYear ?? 0
            }).Cast<object>().ToList();

            var data = new
            {
                username = traktUser.UserName,
                password = traktUser.PasswordHash,
                movies = moviesPayload
            };

            var options = new HttpRequestOptions
                                                {
                                                    RequestContent = _jsonSerializer.SerializeToString(data),
                                                    ResourcePool = Plugin.Instance.TraktResourcePool,
                                                    CancellationToken = cancellationToken,
                                                    Url = seen ? TraktUris.MovieSeen : TraktUris.MovieUnSeen
                                                };

            var response = await _httpClient.Post(options).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response.Content);
        }



        /// <summary>
        /// Send a list of episodes to trakt.tv that have been marked watched or unwatched
        /// </summary>
        /// <param name="episodes">The list of episodes to send</param>
        /// <param name="traktUser">The trakt user profile that is being updated</param>
        /// <param name="seen">True if episodes are being marked seen, false otherwise</param>
        /// <param name="cancellationToken">The Cancellation Token</param>
        /// <returns></returns>
        public async Task<List<TraktResponseDataContract>> SendEpisodePlaystateUpdates(List<Episode> episodes, TraktUser traktUser, bool seen, CancellationToken cancellationToken)
        {
            if (episodes == null)
                throw new ArgumentNullException("episodes");

            if (traktUser == null)
                throw new ArgumentNullException("traktUser");

            var episodesPayload = episodes.Select(ep => new
            {
                season = ep.ParentIndexNumber,
                episode = ep.IndexNumber
            }).Cast<object>().ToList();

            var traktResponses = new List<TraktResponseDataContract>();

            var payloadParcel = new List<object>();

            foreach (var episode in episodesPayload)
            {
                payloadParcel.Add(episode);

                if (payloadParcel.Count == 100)
                {
                    var response = await
                        SendEpisodePlaystateUpdatesInternalAsync(payloadParcel, episodes[0].Series, traktUser, seen,
                            cancellationToken);

                    if (response != null)
                        traktResponses.Add(response);

                    payloadParcel.Clear();
                }
            }

            if (payloadParcel.Count > 0)
            {
                var response = await
                        SendEpisodePlaystateUpdatesInternalAsync(payloadParcel, episodes[0].Series, traktUser, seen,
                            cancellationToken);

                if (response != null)
                    traktResponses.Add(response);
            }

            return traktResponses;
        }


        private async Task<TraktResponseDataContract> SendEpisodePlaystateUpdatesInternalAsync(List<object> episodesPayload, Series series, TraktUser traktUser, bool seen, CancellationToken cancellationToken)
        {
            var data = new
            {
                username = traktUser.UserName,
                password = traktUser.PasswordHash,
                imdb_id = series.GetProviderId(MetadataProviders.Imdb),
                tvdb_id = series.GetProviderId(MetadataProviders.Tvdb),
                title = series.Name,
                year = (series.ProductionYear ?? 0).ToString(CultureInfo.InvariantCulture),
                episodes = episodesPayload
            };

            var options = new HttpRequestOptions
            {
                RequestContent = _jsonSerializer.SerializeToString(data),
                ResourcePool = Plugin.Instance.TraktResourcePool,
                CancellationToken = cancellationToken,
                Url = seen ? TraktUris.ShowEpisodeSeen : TraktUris.ShowEpisodeUnSeen
            };

            var response = await _httpClient.Post(options).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response.Content);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<TraktResponseDataContract> SendCancelWatchingMovie(TraktUser traktUser)
        {
            var data = new Dictionary<string, string>
                           {
                               {"username", traktUser.UserName},
                               {"password", traktUser.PasswordHash}
                           };

            var response =
                await
                _httpClient.Post(TraktUris.MovieCancelWatching, data, Plugin.Instance.TraktResourcePool,
                                 CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<TraktResponseDataContract> SendCancelWatchingShow(TraktUser traktUser)
        {
            var data = new Dictionary<string, string>
                           {
                               {"username", traktUser.UserName},
                               {"password", traktUser.PasswordHash}
                           };

            var response =
                await
                _httpClient.Post(TraktUris.ShowCancelWatching, data, Plugin.Instance.TraktResourcePool,
                                 CancellationToken.None).ConfigureAwait(false);

            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response);
        }



        /// <summary>
        /// Delete all media from a users trakt.tv library, including watch history and then add all media that's stored
        /// in the MB server library to the trakt.tv library.
        /// Intended to be used to clean a users library. THIS IS A DESTRUCTIVE EVENT.
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task ResetTraktTvLibrary(TraktUser traktUser)
        {
            // Get a list of all the media in a users library
            var allMovies = await SendGetAllMoviesRequest(traktUser).ConfigureAwait(false);
            var allShows = await SendGetCollectionShowsRequest(traktUser).ConfigureAwait(false);

            // then delete them all

            if (allMovies != null && allMovies.Any())
            {
                
            }

            if (allShows != null && allShows.Any())
            {
                foreach (var show in allShows)
                {
                    var data = new
                    {
                        username = traktUser.UserName,
                        password = traktUser.PasswordHash,
                        tvdb_id = show.TvdbId,
                        title = show.Title,
                        year = show.Year
                    };

                    var options = new HttpRequestOptions
                    {
                        RequestContent = _jsonSerializer.SerializeToString(data),
                        ResourcePool = Plugin.Instance.TraktResourcePool,
                        CancellationToken = CancellationToken.None,
                        Url = TraktUris.ShowUnLibrary
                    };

                    await _httpClient.Post(options).ConfigureAwait(false);
                }
                
            }

            // How to manually run the 'SyncLibraryTask' so that we add back a 'clean' library?


        }
    }
}
