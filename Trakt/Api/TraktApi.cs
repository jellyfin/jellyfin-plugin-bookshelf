using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Library;
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
        private readonly IUserDataManager _userDataManager;
        private readonly IFileSystem _fileSystem;

        public TraktApi(IJsonSerializer jsonSerializer, ILogger logger, IHttpClient httpClient,
            IServerApplicationHost appHost, IUserDataManager userDataManager, IFileSystem fileSystem)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            _userDataManager = userDataManager;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

        public bool CanSync(BaseItem item, TraktUser traktUser)
        {
            if (item.Path == null || item.LocationType == LocationType.Virtual)
            {
                return false;
            }

            if (traktUser.LocationsExcluded != null && traktUser.LocationsExcluded.Any(s => _fileSystem.ContainsSubPath(s, item.Path)))
            {
                return false;
            }

            var movie = item as Movie;

            if (movie != null)
            {
                return !string.IsNullOrEmpty(movie.GetProviderId(MetadataProviders.Imdb)) ||
                    !string.IsNullOrEmpty(movie.GetProviderId(MetadataProviders.Tmdb));
            }

            var episode = item as Episode;

            if (episode != null && episode.Series != null && !episode.IsVirtualUnaired && !episode.IsMissingEpisode && (episode.IndexNumber.HasValue || !string.IsNullOrEmpty(episode.GetProviderId(MetadataProviders.Tvdb))))
            {
                var series = episode.Series;

                return !string.IsNullOrEmpty(series.GetProviderId(MetadataProviders.Imdb)) ||
                    !string.IsNullOrEmpty(series.GetProviderId(MetadataProviders.Tvdb));
            }

            return false;
        }
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

            using (var response = await PostToTrakt(url, movieData, CancellationToken.None, traktUser))
            {
                return _jsonSerializer.DeserializeFromStream<TraktScrobbleResponse>(response);
            }
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
            var tvDbId = episode.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(tvDbId) && (!episode.IndexNumber.HasValue || !episode.IndexNumberEnd.HasValue || episode.IndexNumberEnd <= episode.IndexNumber))
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
                            Tvdb = tvDbId.ConvertToInt()
                        },
                    }
                });
            }
            else if (episode.IndexNumber.HasValue)
            {
                var indexNumber = episode.IndexNumber.Value;
                var finalNumber = (episode.IndexNumberEnd ?? episode.IndexNumber).Value;

                for (var number = indexNumber; number <= finalNumber; number++)
                {
                    episodeDatas.Add(new TraktScrobbleEpisode
                    {
                        AppDate = DateTime.Today.ToString("yyyy-MM-dd"),
                        AppVersion = _appHost.ApplicationVersion.ToString(),
                        Progress = progressPercent,
                        Episode = new TraktEpisode
                        {
                            Season = episode.GetSeasonNumber(),
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
                    });
                }
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
                using (var response = await PostToTrakt(url, traktScrobbleEpisode, CancellationToken.None, traktUser))
                {
                    responses.Add(_jsonSerializer.DeserializeFromStream<TraktScrobbleResponse>(response));
                }
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
                var traktMovieCollected = new TraktMovieCollected
                {
                    CollectedAt = m.DateCreated.ToISO8601(),
                    Title = m.Name,
                    Year = m.ProductionYear,
                    Ids = new TraktMovieId
                    {
                        Imdb = m.GetProviderId(MetadataProviders.Imdb),
                        Tmdb = m.GetProviderId(MetadataProviders.Tmdb).ConvertToInt()
                    }
                };
                if (traktUser.ExportMediaInfo)
                {
                    traktMovieCollected.Is3D = m.Is3D;
                    traktMovieCollected.AudioChannels = audioStream.GetAudioChannels();
                    traktMovieCollected.Audio = audioStream.GetCodecRepresetation();
                    traktMovieCollected.Resolution = m.GetDefaultVideoStream().GetResolution();
                }
                return traktMovieCollected;
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
                using (var response = await PostToTrakt(url, data, cancellationToken, traktUser))
                {
                    responses.Add(_jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response));
                }
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

        private async Task<TraktSyncResponse> SendLibraryUpdateInternalAsync(IEnumerable<Episode> episodes,
            TraktUser traktUser, CancellationToken cancellationToken, EventType eventType)
        {
            var episodesPayload = new List<TraktEpisodeCollected>();
            var showPayload = new List<TraktShowCollected>();
            foreach (Episode episode in episodes)
            {
                var audioStream = episode.GetMediaStreams().FirstOrDefault(x => x.Type == MediaStreamType.Audio);
                var tvDbId = episode.GetProviderId(MetadataProviders.Tvdb);

                if (!string.IsNullOrEmpty(tvDbId) &&
                    (!episode.IndexNumber.HasValue || !episode.IndexNumberEnd.HasValue ||
                     episode.IndexNumberEnd <= episode.IndexNumber))
                {
                    var traktEpisodeCollected = new TraktEpisodeCollected
                    {
                        CollectedAt = episode.DateCreated.ToISO8601(),
                        Ids = new TraktEpisodeId
                        {
                            Tvdb = tvDbId.ConvertToInt()
                        }
                    };
                    if (traktUser.ExportMediaInfo)
                    {
                        traktEpisodeCollected.Is3D = episode.Is3D;
                        traktEpisodeCollected.AudioChannels = audioStream.GetAudioChannels();
                        traktEpisodeCollected.Audio = audioStream.GetCodecRepresetation();
                        traktEpisodeCollected.Resolution = episode.GetDefaultVideoStream().GetResolution();
                    }
                    episodesPayload.Add(traktEpisodeCollected);
                }
                else if (episode.IndexNumber.HasValue)
                {
                    var indexNumber = episode.IndexNumber.Value;
                    var finalNumber = (episode.IndexNumberEnd ?? episode.IndexNumber).Value;
                    var syncShow =
                        showPayload.FirstOrDefault(
                            sre =>
                                sre.Ids != null &&
                                sre.Ids.Tvdb == episode.Series.GetProviderId(MetadataProviders.Tvdb).ConvertToInt());
                    if (syncShow == null)
                    {
                        syncShow = new TraktShowCollected
                        {
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
                        syncShow.Seasons.FirstOrDefault(ss => ss.Number == episode.GetSeasonNumber());
                    if (syncSeason == null)
                    {
                        syncSeason = new TraktShowCollected.TraktSeasonCollected
                        {
                            Number = episode.GetSeasonNumber(),
                            Episodes = new List<TraktEpisodeCollected>()
                        };
                        syncShow.Seasons.Add(syncSeason);
                    }
                    for (var number = indexNumber; number <= finalNumber; number++)
                    {
                        var traktEpisodeCollected = new TraktEpisodeCollected
                        {
                            Number = number,
                            CollectedAt = episode.DateCreated.ToISO8601(),
                            Ids = new TraktEpisodeId
                            {
                                Tvdb = tvDbId.ConvertToInt()
                            }
                        };
                        if (traktUser.ExportMediaInfo)
                        {
                            traktEpisodeCollected.Is3D = episode.Is3D;
                            traktEpisodeCollected.AudioChannels = audioStream.GetAudioChannels();
                            traktEpisodeCollected.Audio = audioStream.GetCodecRepresetation();
                            traktEpisodeCollected.Resolution = episode.GetDefaultVideoStream().GetResolution();
                        }
                        syncSeason.Episodes.Add(traktEpisodeCollected);
                    }
                }
            }

            var data = new TraktSyncCollected
            {
                Episodes = episodesPayload.ToList(),
                Shows = showPayload.ToList()
            };

            var url = eventType == EventType.Add ? TraktUris.SyncCollectionAdd : TraktUris.SyncCollectionRemove;
            using (var response = await PostToTrakt(url, data, cancellationToken, traktUser))
            {
                return _jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response);
            }
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
            using (var response = await PostToTrakt(url, data, cancellationToken, traktUser))
            {
                return _jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response);
            }
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
                        var indexNumber = episode.IndexNumber.Value;
                        var show = new TraktShowRated
                        {
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
                                    Number = episode.GetSeasonNumber(),
                                    Episodes = new List<TraktEpisodeRated>
                                    {
                                        new TraktEpisodeRated
                                        {
                                            Number = indexNumber,
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

            using (var response = await PostToTrakt(TraktUris.SyncRatingsAdd, data, traktUser))
            {
                return _jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response);
            }
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
            using (var response = await GetFromTrakt(TraktUris.RecommendationsMovies, traktUser))
            {
                return _jsonSerializer.DeserializeFromStream<List<TraktMovie>>(response);
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<TraktShow>> SendShowRecommendationsRequest(TraktUser traktUser)
        {
            using (var response = await GetFromTrakt(TraktUris.RecommendationsShows, traktUser))
            {
                return _jsonSerializer.DeserializeFromStream<List<TraktShow>>(response);
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<DataContracts.Users.Watched.TraktMovieWatched>> SendGetAllWatchedMoviesRequest(TraktUser traktUser)
        {
            using (var response = await GetFromTrakt(TraktUris.WatchedMovies, traktUser))
            {
                return _jsonSerializer.DeserializeFromStream<List<DataContracts.Users.Watched.TraktMovieWatched>>(response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<DataContracts.Users.Watched.TraktShowWatched>> SendGetWatchedShowsRequest(TraktUser traktUser)
        {
            using (var response = await GetFromTrakt(TraktUris.WatchedShows, traktUser))
            {
                return _jsonSerializer.DeserializeFromStream<List<DataContracts.Users.Watched.TraktShowWatched>>(response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<DataContracts.Users.Collection.TraktMovieCollected>> SendGetAllCollectedMoviesRequest(TraktUser traktUser)
        {
            using (var response = await GetFromTrakt(TraktUris.CollectedMovies, traktUser))
            {
                return _jsonSerializer.DeserializeFromStream<List<DataContracts.Users.Collection.TraktMovieCollected>>(response);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traktUser"></param>
        /// <returns></returns>
        public async Task<List<DataContracts.Users.Collection.TraktShowCollected>> SendGetCollectedShowsRequest(TraktUser traktUser)
        {
            using (var response = await GetFromTrakt(TraktUris.CollectedShows, traktUser))
            {
                return _jsonSerializer.DeserializeFromStream<List<DataContracts.Users.Collection.TraktShowCollected>>(response);
            }
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

            var moviesPayload = movies.Select(m =>
            {
                var lastPlayedDate = seen
                    ? _userDataManager.GetUserData(new Guid(traktUser.LinkedMbUserId), m.GetUserDataKey()).LastPlayedDate
                    : null;
                return new TraktMovieWatched
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
                    Year = m.ProductionYear,
                    WatchedAt = lastPlayedDate.HasValue ? lastPlayedDate.Value.ToISO8601() : null
                };
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

                using (var response = await PostToTrakt(url, data, cancellationToken, traktUser))
                {
                    if (response != null)
                        traktResponses.Add(_jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response));
                }
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
                var response = await SendEpisodePlaystateUpdatesInternalAsync(chunk, traktUser, seen, cancellationToken);

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
                var lastPlayedDate = seen
                    ? _userDataManager.GetUserData(new Guid(traktUser.LinkedMbUserId), episode.GetUserDataKey())
                        .LastPlayedDate
                    : null;
                if (!string.IsNullOrEmpty(tvDbId) && (!episode.IndexNumber.HasValue || !episode.IndexNumberEnd.HasValue || episode.IndexNumberEnd <= episode.IndexNumber))
                {

                    data.Episodes.Add(new TraktEpisodeWatched
                    {
                        Ids = new TraktEpisodeId
                        {
                            Tvdb = int.Parse(tvDbId)
                        },
                        WatchedAt = lastPlayedDate.HasValue ? lastPlayedDate.Value.ToISO8601() : null
                    });
                }
                else if (episode.IndexNumber != null)
                {
                    var indexNumber = episode.IndexNumber.Value;
                    var finalNumber = (episode.IndexNumberEnd ?? episode.IndexNumber).Value;

                    var syncShow = data.Shows.FirstOrDefault(sre => sre.Ids != null && sre.Ids.Tvdb == episode.Series.GetProviderId(MetadataProviders.Tvdb).ConvertToInt());
                    if (syncShow == null)
                    {
                        syncShow = new TraktShowWatched
                        {
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
                    var syncSeason = syncShow.Seasons.FirstOrDefault(ss => ss.Number == episode.GetSeasonNumber());
                    if(syncSeason == null)
                    {
                        syncSeason = new TraktSeasonWatched
                        {
                            Number = episode.GetSeasonNumber(),
                            Episodes = new List<TraktEpisodeWatched>()
                        };
                        syncShow.Seasons.Add(syncSeason);
                    }
                    for (var number = indexNumber; number <= finalNumber; number++)
                    {
                        syncSeason.Episodes.Add(new TraktEpisodeWatched
                        {
                            Number = number,
                            WatchedAt = lastPlayedDate.HasValue ? lastPlayedDate.Value.ToISO8601() : null
                        });
                    }
                }
            }
            var url = seen ? TraktUris.SyncWatchedHistoryAdd : TraktUris.SyncWatchedHistoryRemove;

            using (var response = await PostToTrakt(url, data, cancellationToken, traktUser))
            {
                return _jsonSerializer.DeserializeFromStream<TraktSyncResponse>(response);
            }
        }

        public async Task<TraktUserToken> GetUserToken(TraktUser traktUser)
        {
            var data = new TraktUserTokenRequest
            {
                Login = traktUser.UserName,
                Password = traktUser.Password
            };

            using (var response = await PostToTrakt(TraktUris.Login, data, null))
            {
                return _jsonSerializer.DeserializeFromStream<TraktUserToken>(response);
            }
        }

        private Task<Stream> GetFromTrakt(string url, TraktUser traktUser)
        {
            return GetFromTrakt(url, CancellationToken.None, traktUser);
        }

        private async Task<Stream> GetFromTrakt(string url, CancellationToken cancellationToken, TraktUser traktUser)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                ResourcePool = Plugin.Instance.TraktResourcePool,
                CancellationToken = cancellationToken,
                RequestContentType = "application/json",
                TimeoutMs = 120000,
                LogErrorResponseBody = false,
                LogRequest = true
            };
            await SetRequestHeaders(options, traktUser);

            try
            {
                return await _httpClient.Get(options).ConfigureAwait(false);
            }
            catch
            {
                
            }

            // Retry
            return await _httpClient.Get(options).ConfigureAwait(false);
        }

        private Task<Stream> PostToTrakt(string url, object data, TraktUser traktUser)
        {
            return PostToTrakt(url, data, CancellationToken.None, traktUser);
        }

        private async Task<Stream> PostToTrakt(string url, object data, CancellationToken cancellationToken, TraktUser traktUser)
        {
            var requestContent = data == null? string.Empty : _jsonSerializer.SerializeToString(data);
            if (traktUser != null && traktUser.ExtraLogging && url != TraktUris.Login)
            {
                _logger.Debug(requestContent);
            }
            var options = new HttpRequestOptions
            {
                Url = url,
                ResourcePool = Plugin.Instance.TraktResourcePool,
                CancellationToken = cancellationToken,
                RequestContentType = "application/json",
                RequestContent = requestContent,
                TimeoutMs = 120000,
                LogErrorResponseBody = false,
                LogRequest = true
            };
            await SetRequestHeaders(options, traktUser);

            try
            {
                var response = await _httpClient.Post(options).ConfigureAwait(false);
                return response.Content;
            }
            catch
            {
                
            }

            // retry
            var retryResponse = await _httpClient.Post(options).ConfigureAwait(false);
            return retryResponse.Content;
        }

        private async Task SetRequestHeaders(HttpRequestOptions options, TraktUser traktUser)
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
