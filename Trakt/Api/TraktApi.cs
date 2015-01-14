using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using Trakt.Api.DataContracts;
using Trakt.Api.DataContracts.BaseModel;
using Trakt.Api.DataContracts.Scrobble;
using Trakt.Api.DataContracts.Sync;
using Trakt.Api.DataContracts.Sync.Ratings;
using Trakt.Api.DataContracts.Sync.Watched;
using Trakt.Helpers;
using Trakt.Model;
using MediaBrowser.Model.Entities;
using TraktMovieCollected = Trakt.Api.DataContracts.Sync.Collection.TraktMovieCollected;
using TraktEpisodeCollected = Trakt.Api.DataContracts.Sync.Collection.TraktEpisodeCollected;
using TraktShowCollected = Trakt.Api.DataContracts.Sync.Collection.TraktShowCollected;

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
        private readonly IServerApplicationHost _appHost;

        public TraktApi(IJsonSerializer jsonSerializer, ILogger logger, IHttpClient httpClient, IServerApplicationHost appHost)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

//        /// <summary>
//        /// Return information about the user, including ratings format
//        /// </summary>
//        /// <param name="traktUser"></param>
//        /// <returns></returns>
//        public async Task<AccountSettingsDataContract> GetUserAccount(TraktUser traktUser)
//        {
//            var data = new Dictionary<string, string> { { "username", traktUser.UserName }, { "password", traktUser.Password } };
//
//            var response =
//                await
//                _httpClient.Post(TraktUris.AccountSettings, data, Plugin.Instance.TraktResourcePool,
//                                                                     CancellationToken.None).ConfigureAwait(false);
//
//            return _jsonSerializer.DeserializeFromStream<AccountSettingsDataContract>(response);
//        }
//
//
//
//        /// <summary>
//        /// Return a list of the users friends
//        /// </summary>
//        /// <param name="traktUser">The user who's friends you want to retrieve</param>
//        /// <returns>A TraktFriendDataContract</returns>
//        public async Task<TraktFriendDataContract> GetUserFriends(TraktUser traktUser)
//        {
//            var data = new Dictionary<string, string> { { "username", traktUser.UserName }, { "password", traktUser.Password } };
//
//            var response = await _httpClient.Post(string.Format(TraktUris.Friends, traktUser.UserName), data, Plugin.Instance.TraktResourcePool,
//                                                                     CancellationToken.None).ConfigureAwait(false);
//
//            return _jsonSerializer.DeserializeFromStream<TraktFriendDataContract>(response);
//            
//        }
//
//
//
        /// <summary>
        /// Report to trakt.tv that a movie is being watched, or has been watched.
        /// </summary>
        /// <param name="movie">The movie being watched/scrobbled</param>
        /// <param name="mediaStatus">MediaStatus enum dictating whether item is being watched or scrobbled</param>
        /// <param name="traktUser">The user that watching the current movie</param>
        /// <param name="progressPercent"></param>
        /// <returns>A standard TraktResponse Data Contract</returns>
        public async Task<TraktScrobbleResponse> SendMovieStatusUpdateAsync(Movie movie, MediaStatus mediaStatus, TraktUser traktUser, float progressPercent)
        {
            var movieData = new TraktScrobbleMovie
            {
                AppDate = DateTime.Today.ToString("yyyy-MM-dd"),
                AppVersion = _appHost.ApplicationVersion.ToString(),
                Progress = progressPercent,
                Movie = new TraktMovie
                {
                    Title = movie.Name,
                    Year = movie.ProductionYear,
                    Ids = new TraktMovieId
                    {
                        Imdb = movie.GetProviderId(MetadataProviders.Imdb),
                        Tmdb = movie.GetProviderId(MetadataProviders.Tmdb).ConvertToInt()
                    }
                }
            };

            string url;
            switch (mediaStatus)
            {
                case MediaStatus.Watching:
                    url = TraktUris.ScrobbleStart;
                    break;
                case MediaStatus.Paused:
                    url = TraktUris.ScrobblePause;
                    break;
                default:
                    url = TraktUris.ScrobbleStop;
                    break;
            }

            var response = await PostToTrakt(url, movieData, CancellationToken.None, traktUser);
            return _jsonSerializer.DeserializeFromStream<TraktScrobbleResponse>(response);
        }


        /// <summary>
        /// Reports to trakt.tv that an episode is being watched. Or that Episode(s) have been watched.
        /// </summary>
        /// <param name="episode">The episode being watched</param>
        /// <param name="status">Enum indicating whether an episode is being watched or scrobbled</param>
        /// <param name="traktUser">The user that's watching the episode</param>
        /// <param name="progressPercent"></param>
        /// <returns>A List of standard TraktResponse Data Contracts</returns>
        public async Task<List<TraktScrobbleResponse>> SendEpisodeStatusUpdateAsync(Episode episode, MediaStatus status, TraktUser traktUser, float progressPercent)
        {
            var episodeDatas = new List<TraktScrobbleEpisode>();

            if ((episode.IndexNumberEnd == null || episode.IndexNumberEnd == episode.IndexNumber) &&
                !string.IsNullOrEmpty(episode.GetProviderId(MetadataProviders.Tvdb)))
            {
                episodeDatas.Add(new TraktScrobbleEpisode
                {
                    AppDate = DateTime.Today.ToString("yyyy-MM-dd"),
                    AppVersion = _appHost.ApplicationVersion.ToString(),
                    Progress = progressPercent,
                    Episode = new TraktEpisode
                    {
                        Ids = new TraktEpisodeId
                        {
                            Tvdb = episode.GetProviderId(MetadataProviders.Tvdb).ConvertToInt()
                        },
                    }
                });
            }
            // It's a multi-episode file. Add all episodes
            else if (episode.IndexNumber.HasValue)
            {
                episodeDatas.AddRange(Enumerable.Range(episode.IndexNumber.Value,
                    ((episode.IndexNumberEnd ?? episode.IndexNumber).Value -
                     episode.IndexNumber.Value) + 1)
                    .Select(number => new TraktScrobbleEpisode
                    {
                        AppDate = DateTime.Today.ToString("yyyy-MM-dd"),
                        AppVersion = _appHost.ApplicationVersion.ToString(),
                        Progress = progressPercent,
                        Episode = new TraktEpisode
                        {
                            Season = episode.ParentIndexNumber ?? -1,
                            Number = number
                        },
                        Show = new TraktShow
                        {
                            Title = episode.Series.Name,
                            Year = episode.Series.ProductionYear,
                            Ids = new TraktShowId
                            {
                                Tvdb = episode.Series.GetProviderId(MetadataProviders.Tvdb).ConvertToInt(),
                                Imdb = episode.Series.GetProviderId(MetadataProviders.Imdb),
                                TvRage = episode.Series.GetProviderId(MetadataProviders.TvRage).ConvertToInt()
                            }
                        }
                    }).ToList());
            }

            string url;
            switch (status)
            {
                case MediaStatus.Watching:
                    url = TraktUris.ScrobbleStart;
                    break;
                case MediaStatus.Paused:
                    url = TraktUris.ScrobblePause;
                    break;
                default:
                    url = TraktUris.ScrobbleStop;
                    break;
            }
            var responses = new List<TraktScrobbleResponse>();
            foreach (var traktScrobbleEpisode in episodeDatas)
            {
                var response = await PostToTrakt(url, traktScrobbleEpisode, CancellationToken.None, traktUser);
                responses.Add(_jsonSerializer.DeserializeFromStream<TraktScrobbleResponse>(response));
            }
            return responses;
        }

        /// <summary>
        /// Add or remove a list of movies to/from the users trakt.tv library
        /// </summary>
        /// <param name="movies">The movies to add</param>
        /// <param name="traktUser">The user who's library is being updated</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="eventType"></param>
        /// <returns>Task{TraktResponseDataContract}.</returns>
        public async Task<IEnumerable<TraktSyncResponse>> SendLibraryUpdateAsync(List<Movie> movies, TraktUser traktUser,
            CancellationToken cancellationToken, EventType eventType)
        {
            if (movies == null)
                throw new ArgumentNullException("movies");
            if (traktUser == null)
                throw new ArgumentNullException("traktUser");

            if (eventType == EventType.Update) return null;

            var moviesPayload = movies.Select(m =>
            {
                var audioStream = m.GetMediaStreams().FirstOrDefault(x => x.Type == MediaStreamType.Audio);
                return new TraktMovieCollected
                {
                    CollectedAt = m.DateCreated.ToUniversalTime().ToISO8601(),
                    Is3D = m.Is3D,
                    AudioChannels = audioStream.GetAudioChannels(),
                    Audio = audioStream != null ? audioStream.Codec.ToLower().Replace(" ", "_") : null,
                    Resolution = m.GetDefaultVideoStream().GetResolution(),
                    Title = m.Name,
                    Year = m.ProductionYear,
                    Ids = new TraktMovieId
                    {
                        Imdb = m.GetProviderId(MetadataProviders.Imdb),
                        Tmdb = m.GetProviderId(MetadataProviders.Tmdb).ConvertToInt()
                    }
                };
            }).ToList();
            var url = eventType == EventType.Add ? TraktUris.SyncCollectionAdd : TraktUris.SyncCollectionRemove;

            var responses = new List<TraktSyncResponse>();
            var chunks = moviesPayload.ToChunks(100);
            foreach (var chunk in chunks)
            {
                var data = new TraktSyncCollected
                {
                    Movies = chunk.ToList()
                };
                var response = await PostToTrakt(url, data, cancellationToken, traktUser);
                responses.Add(_jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response));
            }
            return responses;
        }



        /// <summary>
        /// Add or remove a list of Episodes to/from the users trakt.tv library
        /// </summary>
        /// <param name="episodes">The episodes to add</param>
        /// <param name="traktUser">The user who's library is being updated</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="eventType"></param>
        /// <returns>Task{TraktResponseDataContract}.</returns>
        public async Task<IEnumerable<TraktSyncResponse>> SendLibraryUpdateAsync(IReadOnlyList<Episode> episodes,
            TraktUser traktUser, CancellationToken cancellationToken, EventType eventType)
        {
            if (episodes == null)
                throw new ArgumentNullException("episodes");

            if (traktUser == null)
                throw new ArgumentNullException("traktUser");

            if (eventType == EventType.Update) return null;
            var responses = new List<TraktSyncResponse>();
            var chunks = episodes.ToChunks(100);
            foreach (var chunk in chunks)
            {
                responses.Add(await SendLibraryUpdateInternalAsync(chunk.ToList(), traktUser, cancellationToken, eventType));
            }
            return responses;
        }

        private async Task<TraktSyncResponse> SendLibraryUpdateInternalAsync(IReadOnlyList<Episode> episodes,
            TraktUser traktUser, CancellationToken cancellationToken, EventType eventType)
        {
            var episodesPayload = new List<TraktEpisodeCollected>();
            var showPayload = new List<TraktShowCollected>();
            foreach (var episode in episodes)
            {
                var audioStream = episode.GetMediaStreams().FirstOrDefault(x => x.Type == MediaStreamType.Audio);
                if ((episode.IndexNumberEnd == null || episode.IndexNumberEnd == episode.IndexNumber) &&
                    !string.IsNullOrEmpty(episode.GetProviderId(MetadataProviders.Tvdb)))
                {
                    episodesPayload.Add(new TraktEpisodeCollected
                    {
                        CollectedAt = episode.DateCreated.ToUniversalTime().ToISO8601(),
                        Ids = new TraktEpisodeId
                        {
                            Tvdb = episode.GetProviderId(MetadataProviders.Tvdb).ConvertToInt()
                        },
                        Is3D = episode.Is3D,
                        AudioChannels = audioStream.GetAudioChannels(),
                        Audio = audioStream != null ? audioStream.Codec.ToLower().Replace(" ", "_") : null,
                        Resolution = episode.GetDefaultVideoStream().GetResolution()
                    });
                }
                    // It's a multi-episode file. Add all episodes
                else if (episode.IndexNumber.HasValue)
                {
                    var syncShow =
                        showPayload.FirstOrDefault(
                            sre =>
                                sre.Ids != null &&
                                sre.Ids.Tvdb == episode.Series.GetProviderId(MetadataProviders.Tvdb).ConvertToInt());
                    if (syncShow == null)
                    {
                        syncShow = new TraktShowCollected
                        {
                            Title = episode.Series.Name,
                            Year = episode.Series.ProductionYear,
                            Ids = new TraktShowId
                            {
                                Tvdb = episode.Series.GetProviderId(MetadataProviders.Tvdb).ConvertToInt(),
                                Imdb = episode.Series.GetProviderId(MetadataProviders.Imdb),
                                TvRage = episode.Series.GetProviderId(MetadataProviders.TvRage).ConvertToInt()
                            },
                            Seasons = new List<TraktShowCollected.TraktSeasonCollected>()
                        };
                        showPayload.Add(syncShow);
                    }
                    var syncSeason =
                        syncShow.Seasons.FirstOrDefault(ss => ss.Number == (episode.ParentIndexNumber ?? -1));
                    if (syncSeason == null)
                    {
                        syncSeason = new TraktShowCollected.TraktSeasonCollected
                        {
                            Number = episode.ParentIndexNumber ?? -1,
                            Episodes = new List<TraktEpisodeCollected>()
                        };
                        syncShow.Seasons.Add(syncSeason);
                    }
                    syncSeason.Episodes.AddRange(Enumerable.Range(episode.IndexNumber.Value,
                        ((episode.IndexNumberEnd ?? episode.IndexNumber).Value -
                         episode.IndexNumber.Value) + 1)
                        .Select(number => new TraktEpisodeCollected
                        {
                            Number = number,
                            CollectedAt = episode.DateCreated.ToUniversalTime().ToISO8601(),
                            Ids = new TraktEpisodeId
                            {
                                Tvdb = episode.GetProviderId(MetadataProviders.Tvdb).ConvertToInt()
                            },
                            Is3D = episode.Is3D,
                            AudioChannels = audioStream.GetAudioChannels(),
                            Audio = audioStream != null ? audioStream.Codec.ToLower().Replace(" ", "_") : null,
                            Resolution = episode.GetDefaultVideoStream().GetResolution()
                        })
                        .ToList());
                }
            }

            var data = new TraktSyncCollected
            {
                Episodes = episodesPayload.ToList(),
                Shows = showPayload.ToList()
            };

            var url = eventType == EventType.Add ? TraktUris.SyncCollectionAdd : TraktUris.SyncCollectionRemove;
            var response = await PostToTrakt(url, data, cancellationToken, traktUser);
            return _jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response);
        }



        /// <summary>
        /// Add or remove a Show(Series) to/from the users trakt.tv library
        /// </summary>
        /// <param name="show">The show to remove</param>
        /// <param name="traktUser">The user who's library is being updated</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="eventType"></param>
        /// <returns>Task{TraktResponseDataContract}.</returns>
        public async Task<TraktSyncResponse> SendLibraryUpdateAsync(Series show, TraktUser traktUser, CancellationToken cancellationToken, EventType eventType)
        {
            if (show == null)
                throw new ArgumentNullException("show");
            if (traktUser == null)
                throw new ArgumentNullException("traktUser");

            if (eventType == EventType.Update) return null;

            var showPayload = new List<TraktShowCollected>
            {
                new TraktShowCollected
                {
                    Title = show.Name,
                    Year = show.ProductionYear,
                    Ids = new TraktShowId
                    {
                        Tvdb = show.GetProviderId(MetadataProviders.Tvdb).ConvertToInt(),
                        Imdb = show.GetProviderId(MetadataProviders.Imdb),
                        TvRage = show.GetProviderId(MetadataProviders.TvRage).ConvertToInt()
                    },
                }
            };

            var data = new TraktSyncCollected
            {
                Shows = showPayload.ToList()
            };

            var url = eventType == EventType.Add ? TraktUris.SyncCollectionAdd : TraktUris.SyncCollectionRemove;
            var response = await PostToTrakt(url, data, cancellationToken, traktUser);
            return _jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response);
        }



        /// <summary>
        /// Rate an item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="rating"></param>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<TraktSyncResponse> SendItemRating(BaseItem item, int rating, TraktUser traktUser)
        {
            object data = new {};
            if (item is Movie)
            {
                data = new
                {
                    movies = new[]
                    {
                        new TraktMovieRated
                        {
                            Title = item.Name,
                            Year = item.ProductionYear,
                            Ids = new TraktMovieId
                            {
                                Imdb = item.GetProviderId(MetadataProviders.Imdb),
                                Tmdb = item.GetProviderId(MetadataProviders.Tmdb).ConvertToInt()
                            },
                            Rating = rating
                        }
                    }
                };
                
            }
            else if (item is Episode )
            {
                var episode = item as Episode;

                if (string.IsNullOrEmpty(episode.GetProviderId(MetadataProviders.Tvdb)))
                {
                    if (episode.IndexNumber.HasValue)
                    {
                        var show = new TraktShowRated
                        {
                            Title = episode.Series.Name,
                            Year = episode.Series.ProductionYear,
                            Ids = new TraktShowId
                            {
                                Tvdb = episode.Series.GetProviderId(MetadataProviders.Tvdb).ConvertToInt(),
                                Imdb = episode.Series.GetProviderId(MetadataProviders.Imdb),
                                TvRage = episode.Series.GetProviderId(MetadataProviders.TvRage).ConvertToInt()
                            },
                            Seasons = new List<TraktShowRated.TraktSeasonRated>
                            {
                                new TraktShowRated.TraktSeasonRated
                                {
                                    Number = episode.ParentIndexNumber ?? -1,
                                    Episodes = new List<TraktEpisodeRated>
                                    {
                                        new TraktEpisodeRated
                                        {
                                            Number = episode.IndexNumber.Value,
                                            Rating = rating
                                        }
                                    }
                                }
                            }
                        };
                        data = new
                        {
                            shows = new[]
                            {
                                show
                            }
                        };
                    }
                }
                else
                {
                    data = new
                    {
                        episodes = new[]
                        {
                            new TraktEpisodeRated
                            {
                                Rating = rating,
                                Ids = new TraktEpisodeId
                                {
                                    Tvdb = episode.GetProviderId(MetadataProviders.Tvdb).ConvertToInt()
                                }
                            }
                        }
                    };
                }
            }
            else // It's a Series
            {
                data = new
                {
                    shows = new[]
                    {
                        new TraktShowRated
                        {
                            Rating = rating,
                            Title = item.Name,
                            Year = item.ProductionYear,
                            Ids = new TraktShowId
                            {
                                Imdb = item.GetProviderId(MetadataProviders.Imdb),
                                Tvdb = item.GetProviderId(MetadataProviders.Tvdb).ConvertToInt()
                            }
                        }
                    }
                };
            }
            var response = await PostToTrakt(TraktUris.SyncRatingsAdd, data, traktUser);

            return _jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response);
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
        public async Task<object> SendItemComment(BaseItem item, string comment, bool containsSpoilers, TraktUser traktUser, bool isReview = false)
        {
            return null;
            //TODO: This functionallity is not available yet
//            string url;
//            var data = new Dictionary<string, string>
//                           {
//                               {"username", traktUser.UserName},
//                               {"password", traktUser.Password}
//                           };
//
//            if (item is Movie)
//            {
//                if (item.ProviderIds != null && item.ProviderIds.ContainsKey("Imdb"))
//                    data.Add("imdb_id", item.ProviderIds["Imdb"]);
//                
//                data.Add("title", item.Name);
//                data.Add("year", item.ProductionYear != null ? item.ProductionYear.ToString() : "");
//                url = TraktUris.CommentMovie;
//            }
//            else
//            {
//                var episode = item as Episode;
//                if (episode != null)
//                {
//                    if (episode.Series.ProviderIds != null)
//                    {
//                        if (episode.Series.ProviderIds.ContainsKey("Imdb"))
//                            data.Add("imdb_id", episode.Series.ProviderIds["Imdb"]);
//
//                        if (episode.Series.ProviderIds.ContainsKey("Tvdb"))
//                            data.Add("tvdb_id", episode.Series.ProviderIds["Tvdb"]);
//                    }
//
//                    data.Add("season", episode.AiredSeasonNumber.ToString());
//                    data.Add("episode", episode.IndexNumber.ToString());
//                    url = TraktUris.CommentEpisode;   
//                }
//                else // It's a Series
//                {
//                    data.Add("title", item.Name);
//                    data.Add("year", item.ProductionYear != null ? item.ProductionYear.ToString() : "");
//
//                    if (item.ProviderIds != null)
//                    {
//                        if (item.ProviderIds.ContainsKey("Imdb"))
//                            data.Add("imdb_id", item.ProviderIds["Imdb"]);
//
//                        if (item.ProviderIds.ContainsKey("Tvdb"))
//                            data.Add("tvdb_id", item.ProviderIds["Tvdb"]);
//                    }
//                    
//                    url = TraktUris.CommentShow;
//                }
//            }
//
//            data.Add("comment", comment);
//            data.Add("spoiler", containsSpoilers.ToString());
//            data.Add("review", isReview.ToString());
//
//            Stream response =
//                await
//                _httpClient.Post(url, data, Plugin.Instance.TraktResourcePool,
//                                                 CancellationToken.None).ConfigureAwait(false);
//
//            return _jsonSerializer.DeserializeFromStream<TraktResponseDataContract>(response);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<TraktMovie>> SendMovieRecommendationsRequest(TraktUser traktUser)
        {
            var response = await GetFromTrakt(TraktUris.RecommendationsMovies, traktUser);
            return _jsonSerializer.DeserializeFromStream<List<TraktMovie>>(response);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<TraktShow>> SendShowRecommendationsRequest(TraktUser traktUser)
        {
            var response = await GetFromTrakt(TraktUris.RecommendationsShows, traktUser);
            return _jsonSerializer.DeserializeFromStream<List<TraktShow>>(response);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<DataContracts.Users.Watched.TraktMovieWatched>> SendGetAllWatchedMoviesRequest(TraktUser traktUser)
        {
            var response = await GetFromTrakt(string.Format(TraktUris.WatchedMovies, traktUser.UserName));
            return _jsonSerializer.DeserializeFromStream<List<DataContracts.Users.Watched.TraktMovieWatched>>(response);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<DataContracts.Users.Watched.TraktShowWatched>> SendGetWatchedShowsRequest(TraktUser traktUser)
        {
            var response = await GetFromTrakt(string.Format(TraktUris.WatchedShows, traktUser.UserName));
            return _jsonSerializer.DeserializeFromStream<List<DataContracts.Users.Watched.TraktShowWatched>>(response);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<DataContracts.Users.Collection.TraktMovieCollected>> SendGetAllCollectedMoviesRequest(TraktUser traktUser)
        {
            var response = await GetFromTrakt(string.Format(TraktUris.CollectedMovies, traktUser.UserName));
            return _jsonSerializer.DeserializeFromStream<List<DataContracts.Users.Collection.TraktMovieCollected>>(response);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<DataContracts.Users.Collection.TraktShowCollected>> SendGetCollectedShowsRequest(TraktUser traktUser)
        {
            var response = await GetFromTrakt(string.Format(TraktUris.CollectedShows, traktUser.UserName));
            return _jsonSerializer.DeserializeFromStream<List<DataContracts.Users.Collection.TraktShowCollected>>(response);
        }

        /// <summary>
        /// Send a list of movies to trakt.tv that have been marked watched or unwatched
        /// </summary>
        /// <param name="movies">The list of movies to send</param>
        /// <param name="traktUser">The trakt user profile that is being updated</param>
        /// <param name="seen">True if movies are being marked seen, false otherwise</param>
        /// <param name="cancellationToken">The Cancellation Token</param>
        /// <returns></returns>
        public async Task<List<TraktSyncResponse>> SendMoviePlaystateUpdates(List<Movie> movies, TraktUser traktUser,
            bool seen, CancellationToken cancellationToken)
        {
            if (movies == null)
                throw new ArgumentNullException("movies");
            if (traktUser == null)
                throw new ArgumentNullException("traktUser");

            var moviesPayload = movies.Select(m => new TraktMovieWatched
            {
                Title = m.Name,
                Ids = new TraktMovieId
                {
                    Imdb = m.GetProviderId(MetadataProviders.Imdb),
                    Tmdb =
                        string.IsNullOrEmpty(m.GetProviderId(MetadataProviders.Tmdb))
                            ? (int?) null
                            : int.Parse(m.GetProviderId(MetadataProviders.Tmdb))
                },
                Year = m.ProductionYear
            }).ToList();
            var chunks = moviesPayload.ToChunks(100).ToList();
            var traktResponses = new List<TraktSyncResponse>();

            foreach (var chunk in chunks)
            {
                var data = new TraktSyncWatched
                {
                    Movies = chunk.ToList()
                };
                var url = seen ? TraktUris.SyncWatchedHistoryAdd : TraktUris.SyncWatchedHistoryRemove;

                var response = await PostToTrakt(url, data, cancellationToken, traktUser);
                if (response != null)
                    traktResponses.Add(_jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response));
            }
            return traktResponses;
        }



        /// <summary>
        /// Send a list of episodes to trakt.tv that have been marked watched or unwatched
        /// </summary>
        /// <param name="episodes">The list of episodes to send</param>
        /// <param name="traktUser">The trakt user profile that is being updated</param>
        /// <param name="seen">True if episodes are being marked seen, false otherwise</param>
        /// <param name="cancellationToken">The Cancellation Token</param>
        /// <returns></returns>
        public async Task<List<TraktSyncResponse>> SendEpisodePlaystateUpdates(List<Episode> episodes, TraktUser traktUser, bool seen, CancellationToken cancellationToken)
        {
            if (episodes == null)
                throw new ArgumentNullException("episodes");

            if (traktUser == null)
                throw new ArgumentNullException("traktUser");
            
            
            var chunks = episodes.ToChunks(100).ToList();
            var traktResponses = new List<TraktSyncResponse>();

            foreach (var chunk in chunks)
            {
                var response = await
                        SendEpisodePlaystateUpdatesInternalAsync(chunk, traktUser, seen, cancellationToken);

                if (response != null)
                    traktResponses.Add(response);
            }
            return traktResponses;
        }


        private async Task<TraktSyncResponse> SendEpisodePlaystateUpdatesInternalAsync(IEnumerable<Episode> episodeChunk, TraktUser traktUser, bool seen, CancellationToken cancellationToken)
        {
            var data = new TraktSyncWatched{ Episodes = new List<TraktEpisodeWatched>(), Shows = new List<TraktShowWatched>() };
            foreach (var episode in episodeChunk)
            {
                var tvDbId = episode.GetProviderId(MetadataProviders.Tvdb);

                if (!string.IsNullOrEmpty(tvDbId) && (!episode.IndexNumberEnd.HasValue || episode.IndexNumberEnd == episode.IndexNumber))
                {
                    data.Episodes.Add(new TraktEpisodeWatched
                    {
                        Ids = new TraktEpisodeId
                        {
                            Tvdb = int.Parse(tvDbId)
                        }
                    });
                }
                else if (episode.IndexNumber.HasValue)
                {
                    var syncShow = data.Shows.FirstOrDefault(sre => sre.Ids != null && sre.Ids.Tvdb == episode.Series.GetProviderId(MetadataProviders.Tvdb).ConvertToInt());
                    if (syncShow == null)
                    {
                        syncShow = new TraktShowWatched
                        {
                            Title = episode.Series.Name,
                            Year = episode.Series.ProductionYear,
                            Ids = new TraktShowId
                            {
                                Tvdb = episode.Series.GetProviderId(MetadataProviders.Tvdb).ConvertToInt(),
                                Imdb = episode.Series.GetProviderId(MetadataProviders.Imdb),
                                TvRage = episode.Series.GetProviderId(MetadataProviders.TvRage).ConvertToInt()
                            },
                            Seasons = new List<TraktSeasonWatched>()
                        };
                        data.Shows.Add(syncShow);
                    }
                    var syncSeason = syncShow.Seasons.FirstOrDefault(ss => ss.Number == (episode.ParentIndexNumber??-1));
                    if(syncSeason == null)
                    {
                        syncSeason = new TraktSeasonWatched
                        {
                            Number = episode.ParentIndexNumber ?? -1,
                            Episodes = new List<TraktEpisodeWatched>()
                        };
                        syncShow.Seasons.Add(syncSeason);
                    }
                    syncSeason.Episodes.AddRange(Enumerable.Range(episode.IndexNumber.Value,
                        ((episode.IndexNumberEnd ?? episode.IndexNumber).Value -
                         episode.IndexNumber.Value) + 1)
                        .Select(number => new TraktEpisodeWatched{Number = number})
                        .ToList());
                }
            }
            var url = seen ? TraktUris.SyncWatchedHistoryAdd : TraktUris.SyncWatchedHistoryRemove;
            
            var response = await PostToTrakt(url,data, cancellationToken, traktUser);

            return _jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response);
        }

        public async Task<TraktUserToken> GetUserToken(TraktUser traktUser)
        {
            var data = new TraktUserTokenRequest
            {
                Login = traktUser.UserName,
                Password = traktUser.Password
            };
            var response = await PostToTrakt(TraktUris.Login, data);
            return _jsonSerializer.DeserializeFromStream<TraktUserToken>(response);
        }

        private async Task<Stream> GetFromTrakt(string url, TraktUser traktUser = null)
        {
            return await GetFromTrakt(url, CancellationToken.None, traktUser);
        }

        private async Task<Stream> GetFromTrakt(string url, CancellationToken cancellationToken, TraktUser traktUser = null)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                ResourcePool = Plugin.Instance.TraktResourcePool,
                CancellationToken = cancellationToken,
                RequestContentType = "application/json",
                TimeoutMs = 60000,
            };
            await SetRequestHeaders(options, traktUser);
            var response = await _httpClient.Get(options).ConfigureAwait(false);
            return response;
        }


        private async Task<Stream> PostToTrakt(string url, object data, TraktUser traktUser = null)
        {
            return await PostToTrakt(url, data, CancellationToken.None, traktUser);
        }

        private async Task<Stream> PostToTrakt(string url, object data, CancellationToken cancellationToken, TraktUser traktUser = null)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                ResourcePool = Plugin.Instance.TraktResourcePool,
                CancellationToken = cancellationToken,
                RequestContentType = "application/json",
                RequestContent = data.ToJSON()
            };
            await SetRequestHeaders(options, traktUser);
            // if we're logging in, we don't need to add these headers
            _logger.Debug("\r\n***\r\n"+options.RequestContent+"\r\n***");
            var response = await _httpClient.Post(options).ConfigureAwait(false);
            return response.Content;
        }

        private async Task SetRequestHeaders(HttpRequestOptions options, TraktUser traktUser = null)
        {
            options.RequestHeaders.Add("trakt-api-version", "2");
            options.RequestHeaders.Add("trakt-api-key", TraktUris.Devkey);
            if (traktUser != null)
            {
                if (string.IsNullOrEmpty(traktUser.UserToken))
                {
                    var userToken = await GetUserToken(traktUser);

                    if (userToken != null)
                    {
                        traktUser.UserToken = userToken.Token;
                    }
                }
                if (!string.IsNullOrEmpty(traktUser.UserToken))
                {
                    options.RequestHeaders.Add("trakt-user-login", traktUser.UserName);
                    options.RequestHeaders.Add("trakt-user-token", traktUser.UserToken);
                }
            }
        }
    }
}
