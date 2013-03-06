using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities;
using MoreLinq;
using MediaBrowser.Common.Extensions;

namespace MediaBrowser.Plugins.Dlna.Model
{

    internal abstract class ModelBase
    {
        internal ModelBase(User user, BaseItem mbItem, string id, string parentId)
        {
            this.User = user;
            this.MbItem = mbItem;
            this.Id = id;
            this.ParentId = parentId;
        }
        internal ModelBase(User user, BaseItem mbItem, string id, ModelBase parent)
        {
            this.User = user;
            this.MbItem = mbItem;
            this.Id = id;
            this.Parent = parent;
            if (this.Parent != null)
                this.ParentId = this.Parent.Id;
        }

        protected internal User User { get; private set; }
        protected internal string Id { get; private set; }
        protected internal string ParentId { get; private set; }
        protected internal BaseItem MbItem { get; protected set; }
        protected internal ModelBase Parent { get; private set; }

        protected internal abstract IEnumerable<ModelBase> Children { get; }
        protected internal IEnumerable<ModelBase> GetChildren(int startingIndex, int requestedCount)
        {
            return this.Children.Skip(startingIndex).Take(requestedCount);
        }
        protected internal IEnumerable<ModelBase> RecursiveChildren
        {
            get
            {
                return this.Children.Union(this.Children.SelectMany(i =>
                {
                    if (i == null)
                        return new List<ModelBase>();
                    else
                        return i.RecursiveChildren;
                }));
            }
        }

        protected internal IEnumerable<ModelBase> GetChildrenRecursive(int startingIndex, int requestedCount)
        {
            if (this.Children.Count() >= (startingIndex + requestedCount))
                return this.Children.Skip(startingIndex).Take(requestedCount);
            else
                return this.RecursiveChildren.Skip(startingIndex).Take(requestedCount);
        }

        protected internal ModelBase GetChildRecursive(string id)
        {
            //if we can short cicuit this and find the item in our immediate children, then we don't have to
            //spend extra time doing all that recursive searching, 
            //and of course if we do, our children have the same short cicuit logic because this is a recursive function
            //it seems like it migth result in extra processing but in reality it saves a LOT of processing
            var result = this.Children.FirstOrDefault(i => ((i != null) && (string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase))));
            if (result == null)
                return this.RecursiveChildren.FirstOrDefault(i => ((i != null) && (string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase))));
            else
                return result;
        }

        protected internal abstract Platinum.MediaObject GetMediaObject(Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes);
    }
    internal abstract class ModelBase<T> : ModelBase where T : BaseItem
    {
        internal ModelBase(User user, T mbItem, string parentId)
            : base(user: user, mbItem: mbItem, id: mbItem.Id.ToString(), parentId: parentId)
        {
        }
        internal ModelBase(User user, T mbItem, string id, string parentId)
            : base(user: user, mbItem: mbItem, id: id, parentId: parentId)
        {
        }
        internal ModelBase(User user, T mbItem, ModelBase parent)
            : base(user: user, mbItem: mbItem, id: mbItem.Id.ToString(), parent: parent)
        {
        }
        internal ModelBase(User user, T mbItem, string id, ModelBase parent)
            : base(user: user, mbItem: mbItem, id: id, parent: parent)
        {
        }
        protected internal T MBItem { get { return (T)base.MbItem; } }
    }

    internal abstract class WellKnownContainerBase : ModelBase<Folder>
    {
        internal WellKnownContainerBase(User user, Folder mbItem, string id, string parentId, string title)
            : base(user: user, mbItem: mbItem, id: id, parentId: parentId)
        {
            this.Title = title;
        }
        internal WellKnownContainerBase(User user, Folder mbItem, string id, ModelBase parent, string title)
            : base(user: user, mbItem: mbItem, id: id, parent: parent)
        {
            this.Title = title;
        }
        protected internal string Title { get; private set; }

        protected override internal Platinum.MediaObject GetMediaObject(Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = PlatinumMediaObjectHelper.GetMediaObject(this);
            foreach (var res in PlatinumMediaResourceHelper.GetMediaResource(this, context, urlPrefixes))
            {
                result.AddResource(res);
            }

            foreach (var art in PlatinumAlbumArtInfoHelper.GetAlbumArtInfo(this, context, urlPrefixes))
            {
                result.Extra.AddAlbumArtInfo(art);
            }

            return result;
        }
    }
    internal class Root : WellKnownContainerBase
    {
        internal Root(User user)
            : base(user, user.RootFolder, "0", parent: null, title: "Root")
        {
            this.Music = new WellKnownMusicContainer(user, parent: this);
            this.Video = new WellKnownVideoContainer(user, parent: this);
            this.Playlists = new WellKnownPlaylistsContainer(user, parent: this);
        }
        internal WellKnownMusicContainer Music { get; private set; }
        internal WellKnownVideoContainer Video { get; private set; }
        internal WellKnownPlaylistsContainer Playlists { get; private set; }

        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return new List<ModelBase>() { this.Music, this.Video, this.Playlists };
            }
        }
    }

    internal class WellKnownMusicContainer : WellKnownContainerBase
    {
        internal WellKnownMusicContainer(User user, ModelBase parent)
            : base(user, user.RootFolder, id: "1", parent: parent, title: "Music")
        {
            this.AllMusic = new WellKnownAllMusicContainer(user, parent: this);
            this.Genre = new WellKnownMusicGenreContainer(user);
            this.Artist = new WellKnownMusicArtistContainer(user);
            this.Album = new WellKnownMusicAlbumContainer(user);
        }
        internal WellKnownAllMusicContainer AllMusic { get; private set; }
        internal WellKnownMusicGenreContainer Genre { get; private set; }
        internal WellKnownMusicArtistContainer Artist { get; private set; }
        internal WellKnownMusicAlbumContainer Album { get; private set; }

        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return new List<ModelBase>() { this.AllMusic, this.Genre, this.Artist, this.Album };
            }
        }
    }
    internal class WellKnownVideoContainer : WellKnownContainerBase
    {
        internal WellKnownVideoContainer(User user, ModelBase parent)
            : base(user, user.RootFolder, id: "2", parent: parent, title: "Video")
        {
            this.AllVideo = new WellKnownAllVideoContainer(user);
            this.Genre = new WellKnownVideoGenreContainer(user);
            this.Actor = new WellKnownActorContainer(user);
            this.Series = new WellKnownSeriesContainer(user);
            this.Folders = new WellKnownVideoFolderContainer(user);
        }
        internal WellKnownAllVideoContainer AllVideo { get; private set; }
        internal WellKnownVideoGenreContainer Genre { get; private set; }
        internal WellKnownActorContainer Actor { get; private set; }
        internal WellKnownSeriesContainer Series { get; private set; }
        internal WellKnownVideoFolderContainer Folders { get; private set; }

        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return new List<ModelBase>() { this.AllVideo, this.Genre, this.Actor, this.Series, this.Folders };
            }
        }
    }
    internal class WellKnownPlaylistsContainer : WellKnownContainerBase
    {
        internal WellKnownPlaylistsContainer(User user, ModelBase parent)
            : base(user, user.RootFolder, id: "12", parent: parent, title: "Playlists")
        {
            this.AllPlaylists = new WellKnownAllPlaylistsContainer(user);
            this.PlaylistFolders = new WellKnownPlaylistsFolderContainer(user);
        }
        internal WellKnownAllPlaylistsContainer AllPlaylists { get; private set; }
        internal WellKnownPlaylistsFolderContainer PlaylistFolders { get; private set; }

        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return new List<ModelBase>() { this.AllPlaylists, this.PlaylistFolders };
            }
        }
    }

    internal class WellKnownAllMusicContainer : WellKnownContainerBase
    {
        internal WellKnownAllMusicContainer(User user, ModelBase parent)
            : base(user, user.RootFolder, id: "4", parent: parent, title: "All Music")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return this.MBItem.GetRecursiveChildren(User).OfType<MediaBrowser.Controller.Entities.Audio.Audio>().Select(i => new MusicItem(this.User, i, parentId: this.Id));
            }
        }
    }
    internal class WellKnownMusicGenreContainer : WellKnownContainerBase
    {
        internal WellKnownMusicGenreContainer(User user)
            : base(user, user.RootFolder, id: "5", parentId: "2", title: "Genre")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                var asyncGenres = this.MBItem.GetRecursiveChildren(this.User)
                    .AsParallel()
                    .OfType<MediaBrowser.Controller.Entities.Audio.Audio>()
                    .SelectMany(audio =>
                    {
                        if (audio.Genres == null)
                        {
                            return new string[] { };
                        }
                        return audio.Genres.Where(genre => !string.IsNullOrWhiteSpace(genre));
                    })
                    .DistinctBy(genre => genre, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(genre => genre)
                    .Select(genre => LibraryHelper.GetGenre(genre))
                    .ToList();

                var genres = asyncGenres
                    .Where(i => i != null && !i.IsFaulted && i.IsCompleted)
                    .Select(i => i.Result)
                    .OrderBy(i => i.Name);

                return genres.Select(i => new MusicGenreContainer(this.User, i, parentId: this.Id));

            }
        }

    }
    internal class WellKnownMusicArtistContainer : WellKnownContainerBase
    {
        internal WellKnownMusicArtistContainer(User user)
            : base(user, user.RootFolder, id: "6", parentId: "1", title: "Artist")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return this.MBItem.GetRecursiveChildren(this.User)
                    .OfType<MediaBrowser.Controller.Entities.Audio.MusicArtist>()
                    .DistinctBy(person => person.Name, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(person => person.Name)
                    .Select(i => new MusicArtistContainer(this.User, i, parentId: this.Id));
            }
        }

    }
    internal class WellKnownMusicAlbumContainer : WellKnownContainerBase
    {
        internal WellKnownMusicAlbumContainer(User user)
            : base(user, user.RootFolder, id: "7", parentId: "1", title: "Album")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return this.MBItem.GetRecursiveChildren(this.User)
                    .OfType<MediaBrowser.Controller.Entities.Audio.MusicAlbum>()
                    .DistinctBy(album => album.Name, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(album => album.Name)
                    .Select(album => new MusicAlbumContainer(this.User, album, parentId: this.Id));
            }
        }

    }


    internal class WellKnownAllVideoContainer : WellKnownContainerBase
    {
        internal WellKnownAllVideoContainer(User user)
            : base(user, user.RootFolder, id: "8", parentId: "2", title: "All Video")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return this.MBItem.GetRecursiveChildren(User).OfType<Video>().Select(i => new VideoItem(this.User, i, parentId: this.Id));
            }
        }
    }
    internal class WellKnownVideoGenreContainer : WellKnownContainerBase
    {
        internal WellKnownVideoGenreContainer(User user)
            : base(user, user.RootFolder, id: "9", parentId: "2", title: "Genre")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                var asyncGenres = this.MBItem.GetRecursiveChildren(this.User)
                    .AsParallel()
                    .OfType<Video>()
                    .SelectMany(video =>
                    {
                        if (video.Genres == null)
                        {
                            return new string[] { };
                        }
                        return video.Genres.Where(genre => !string.IsNullOrWhiteSpace(genre));
                    })
                    .DistinctBy(genre => genre, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(genre => genre)
                    .Select(genre => LibraryHelper.GetGenre(genre))
                    .ToList();

                var genres = asyncGenres
                    .Where(i => i != null && !i.IsFaulted && i.IsCompleted)
                    .Select(i => i.Result)
                    .OrderBy(person => person.Name);

                return genres.Select(i => new VideoGenreContainer(this.User, i, parentId: this.Id));
            }
        }

    }
    internal class WellKnownActorContainer : WellKnownContainerBase
    {
        internal WellKnownActorContainer(User user)
            : base(user, user.RootFolder, id: "A", parentId: "2", title: "Actors")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                var asyncPeople = this.MBItem.GetRecursiveChildren(this.User)
                    .AsParallel()
                    .OfType<Video>()
                    .SelectMany(video =>
                    {
                        if (video.People == null)
                            return new PersonInfo[] { };
                        else
                            return video.People.Where(p => string.Equals(p.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(p.Name));
                    })
                    .Where(person => person != null)
                    .DistinctBy(person => person.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(person => LibraryHelper.GetPerson(person))
                    .ToList();

                var people = asyncPeople
                    .Where(i => i != null && !i.IsFaulted && i.IsCompleted)
                    .Select(i => i.Result)
                    .OrderBy(person => person.Name);
                return people.Select(i => new VideoActorContainer(this.User, i, parentId: this.Id));
            }
        }

    }
    internal class WellKnownSeriesContainer : WellKnownContainerBase
    {
        internal WellKnownSeriesContainer(User user)
            : base(user, user.RootFolder, id: "E", parentId: "2", title: "Series")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return this.MBItem.GetRecursiveChildren(this.User)
                    .OfType<MediaBrowser.Controller.Entities.TV.Series>()
                    .DistinctBy(series => series.Name, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(series => series.Name)
                    .Select(series => new VideoSeriesContainer(this.User, series, parentId: this.Id));
            }
        }

    }
    internal class WellKnownVideoFolderContainer : WellKnownContainerBase
    {
        internal WellKnownVideoFolderContainer(User user)
            : base(user, user.RootFolder, id: "15", parentId: "2", title: "Folders")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                var folderChildren = this.MBItem.GetChildren(this.User).OfType<Folder>().Select(i => (ModelBase)(new VideoFolderContainer(this.User, i, this.Id)));
                var videoChildren = this.MBItem.GetChildren(this.User).OfType<Video>().Select(i => (ModelBase)(new VideoItem(this.User, i, this.Id)));
                return folderChildren.Union(videoChildren);
            }
        }
    }

    internal class WellKnownAllPlaylistsContainer : WellKnownContainerBase
    {
        internal WellKnownAllPlaylistsContainer(User user)
            : base(user, user.RootFolder, id: "13", parentId: "12", title: "AllPlaylists")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return new List<ModelBase>();
            }
        }
    }
    internal class WellKnownPlaylistsFolderContainer : WellKnownContainerBase
    {
        internal WellKnownPlaylistsFolderContainer(User user)
            : base(user, user.RootFolder, id: "17", parentId: "12", title: "PlaylistsFolders")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return new List<ModelBase>();
            }
        }
    }

    internal class VideoSeriesContainer : ModelBase<MediaBrowser.Controller.Entities.TV.Series>
    {
        internal VideoSeriesContainer(User user, MediaBrowser.Controller.Entities.TV.Series mbItem, string parentId)
            : base(user, mbItem, parentId)
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                //var folderChildren = this.MBItem.GetChildren(this.User).OfType<Folder>().Select(i => (ModelBase)(new VideoFolderContainer(this.User, i, this.Id)));
                //var videoChildren = this.MBItem.RecursiveChildren.OfType<Video>().Select(i => (ModelBase)(new VideoItem(this.User, i, this.Id)));
                //return folderChildren.Union(videoChildren);

                return this.MBItem.RecursiveChildren.OfType<Video>().Select(i => (ModelBase)(new VideoItem(this.User, i, this.Id)));
            }
        }

        protected override internal Platinum.MediaObject GetMediaObject(Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = PlatinumMediaObjectHelper.GetMediaObject(this);
            foreach (var res in PlatinumMediaResourceHelper.GetMediaResource(this, context, urlPrefixes))
            {
                result.AddResource(res);
            }

            foreach (var art in PlatinumAlbumArtInfoHelper.GetAlbumArtInfo(this, context, urlPrefixes))
            {
                result.Extra.AddAlbumArtInfo(art);
            }

            return result;
        }
    }
    internal class VideoFolderContainer : ModelBase<Folder>
    {
        internal VideoFolderContainer(User user, Folder mbItem, string parentId)
            : base(user, mbItem, parentId)
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                var folderChildren = this.MBItem.GetChildren(this.User).OfType<Folder>().Select(i => (ModelBase)(new VideoFolderContainer(this.User, i, this.Id)));
                var videoChildren = this.MBItem.GetChildren(this.User).OfType<Video>().Select(i => (ModelBase)(new VideoItem(this.User, i, this.Id)));
                return folderChildren.Union(videoChildren);
            }
        }
        protected override internal Platinum.MediaObject GetMediaObject(Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = PlatinumMediaObjectHelper.GetMediaObject(this);
            foreach (var res in PlatinumMediaResourceHelper.GetMediaResource(this, context, urlPrefixes))
            {
                result.AddResource(res);
            }

            foreach (var art in PlatinumAlbumArtInfoHelper.GetAlbumArtInfo(this, context, urlPrefixes))
            {
                result.Extra.AddAlbumArtInfo(art);
            }

            return result;
        }
    }



    internal class VideoGenreContainer : ModelBase<Genre>
    {
        internal VideoGenreContainer(User user, Genre mbItem, string parentId)
            : base(user, mbItem, parentId)
        {
        }

        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                //unfortunately genre isn't a folder like you'd expect, 
                //and it doesn't have children like you'd expect
                //so we have to start at the root and filter everything ourselves
                return this.User.RootFolder.GetRecursiveChildren(User)
                    .OfType<Video>()
                    .Where(i => (i.Genres != null) && i.Genres.Any(g => string.Equals(g, this.MBItem.Name, StringComparison.OrdinalIgnoreCase)))
                    .Select(i => new VideoItem(this.User, i, parentId: this.Id));
            }
        }

        protected internal override Platinum.MediaObject GetMediaObject(Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = PlatinumMediaObjectHelper.GetMediaObject(this);
            foreach (var res in PlatinumMediaResourceHelper.GetMediaResource(this, context, urlPrefixes))
            {
                result.AddResource(res);
            }

            foreach (var art in PlatinumAlbumArtInfoHelper.GetAlbumArtInfo(this, context, urlPrefixes))
            {
                result.Extra.AddAlbumArtInfo(art);
            }

            return result;
        }
    }

    internal class VideoActorContainer : ModelBase<Person>
    {
        internal VideoActorContainer(User user, Person mbItem, string parentId)
            : base(user, mbItem, parentId)
        {
        }

        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                //unfortunately Person doesn't have a children like you'd expect
                //so we have to start at Root and filter everything ourselves
                return this.User.RootFolder.GetRecursiveChildren(User)
                    .OfType<Video>()
                    .Where(i => (i.People != null) && i.People
                        .Any(p => string.Equals(p.Name, this.MBItem.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(p.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase)))
                    .Select(i => new VideoItem(this.User, i, parentId: this.Id));
            }
        }

        protected internal override Platinum.MediaObject GetMediaObject(Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = PlatinumMediaObjectHelper.GetMediaObject(this);
            foreach (var res in PlatinumMediaResourceHelper.GetMediaResource(this, context, urlPrefixes))
            {
                result.AddResource(res);
            }

            foreach (var art in PlatinumAlbumArtInfoHelper.GetAlbumArtInfo(this, context, urlPrefixes))
            {
                result.Extra.AddAlbumArtInfo(art);
            }

            return result;
        }
    }
    internal class VideoItem : ModelBase<Video>
    {
        internal VideoItem(User user, Video mbItem, string parentId)
            : base(user, mbItem, parentId)
        {

        }
        protected internal override IEnumerable<ModelBase> Children { get { return new List<ModelBase>(); } }

        protected internal override Platinum.MediaObject GetMediaObject(Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = PlatinumMediaObjectHelper.GetMediaObject(this, context.Signature);
            foreach (var res in VideoItemPlatinumMediaResourceHelper.GetMediaResource(this, context, urlPrefixes))
            {
                result.AddResource(res);
            }

            foreach (var art in PlatinumAlbumArtInfoHelper.GetAlbumArtInfo(this, context, urlPrefixes))
            {
                result.Extra.AddAlbumArtInfo(art);
            }

            return result;
        }
    }


    internal class MusicItem : ModelBase<MediaBrowser.Controller.Entities.Audio.Audio>
    {
        internal MusicItem(User user, MediaBrowser.Controller.Entities.Audio.Audio mbItem, string parentId)
            : base(user: user, mbItem: mbItem, parentId: parentId)
        {

        }
        protected internal override IEnumerable<ModelBase> Children { get { return new List<ModelBase>(); } }

        protected internal override Platinum.MediaObject GetMediaObject(Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = PlatinumMediaObjectHelper.GetMediaObject(this);
            foreach (var res in MusicItemPlatinumMediaResourceHelper.GetMediaResource(this, context, urlPrefixes))
            {
                result.AddResource(res);
            }

            foreach (var art in PlatinumAlbumArtInfoHelper.GetAlbumArtInfo(this, context, urlPrefixes))
            {
                result.Extra.AddAlbumArtInfo(art);
            }

            return result;
        }

    }
    internal class MusicGenreContainer : ModelBase<Genre>
    {
        internal MusicGenreContainer(User user, Genre mbItem, string parentId)
            : base(user, mbItem, parentId)
        {
        }

        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                //unfortunately genre isn't a folder like you'd expect, 
                //and it doesn't have children like you'd expect
                //so we have to start at the root and filter everything ourselves
                return this.User.RootFolder.GetRecursiveChildren(User)
                    .OfType<MediaBrowser.Controller.Entities.Audio.Audio>()
                    .Where(i => (i.Genres != null) && i.Genres.Any(g => string.Equals(g, this.MBItem.Name, StringComparison.OrdinalIgnoreCase)))
                    .Select(i => new MusicItem(this.User, i, parentId: this.Id));
            }
        }

        protected internal override Platinum.MediaObject GetMediaObject(Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = PlatinumMediaObjectHelper.GetMediaObject(this);
            foreach (var res in PlatinumMediaResourceHelper.GetMediaResource(this, context, urlPrefixes))
            {
                result.AddResource(res);
            }

            foreach (var art in PlatinumAlbumArtInfoHelper.GetAlbumArtInfo(this, context, urlPrefixes))
            {
                result.Extra.AddAlbumArtInfo(art);
            }

            return result;
        }
    }

    internal class MusicArtistContainer : ModelBase<MediaBrowser.Controller.Entities.Audio.MusicArtist>
    {
        internal MusicArtistContainer(User user, MediaBrowser.Controller.Entities.Audio.MusicArtist mbItem, string parentId)
            : base(user: user, mbItem: mbItem, parentId: parentId)
        {

        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return this.User.RootFolder.GetRecursiveChildren(this.User)
                    .OfType<MediaBrowser.Controller.Entities.Audio.Audio>()
                    .Where(i => string.Equals(i.Artist, this.MBItem.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(i => new MusicItem(this.User, i, this.Id));
            }
        }

        protected internal override Platinum.MediaObject GetMediaObject(Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = PlatinumMediaObjectHelper.GetMediaObject(this);
            foreach (var res in PlatinumMediaResourceHelper.GetMediaResource(this, context, urlPrefixes))
            {
                result.AddResource(res);
            }

            foreach (var art in PlatinumAlbumArtInfoHelper.GetAlbumArtInfo(this, context, urlPrefixes))
            {
                result.Extra.AddAlbumArtInfo(art);
            }

            return result;
        }
    }
    internal class MusicAlbumContainer : ModelBase<MediaBrowser.Controller.Entities.Audio.MusicAlbum>
    {
        internal MusicAlbumContainer(User user, MediaBrowser.Controller.Entities.Audio.MusicAlbum mbItem, string parentId)
            : base(user: user, mbItem: mbItem, parentId: parentId)
        {

        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return this.User.RootFolder.GetRecursiveChildren(this.User)
                    .OfType<MediaBrowser.Controller.Entities.Audio.Audio>()
                    .Where(i => string.Equals(i.Album, this.MBItem.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(i => new MusicItem(this.User, i, this.Id));
            }
        }

        protected internal override Platinum.MediaObject GetMediaObject(Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = PlatinumMediaObjectHelper.GetMediaObject(this);
            foreach (var res in PlatinumMediaResourceHelper.GetMediaResource(this, context, urlPrefixes))
            {
                result.AddResource(res);
            }

            foreach (var art in PlatinumAlbumArtInfoHelper.GetAlbumArtInfo(this, context, urlPrefixes))
            {
                result.Extra.AddAlbumArtInfo(art);
            }

            return result;
        }
    }

    internal static class PlatinumMediaObjectHelper
    {
        internal static Platinum.MediaContainer GetMediaObject(WellKnownContainerBase item)
        {
            var result = new Platinum.MediaContainer();
            result.ObjectID = item.Id;
            result.ParentID = item.ParentId;
            result.Class = new Platinum.ObjectClass("object.container", "");
            result.Title = item.Title;
            result.ChildrenCount = item.Children.Count();

            result.Description.Date = item.MbItem.PremiereDate.HasValue ? item.MbItem.PremiereDate.Value.ToString() : string.Empty;
            result.Description.Language = item.MbItem.Language == null ? string.Empty : item.MbItem.Language;
            result.Description.DescriptionText = "this is DescriptionText";
            result.Description.LongDescriptionText = item.MbItem.Overview == null ? string.Empty : item.MbItem.Overview;
            result.Description.Rating = item.MbItem.CommunityRating.ToString();

            return result;
        }

        internal static Platinum.MediaContainer GetMediaObject(VideoSeriesContainer item)
        {
            var result = new Platinum.MediaContainer();
            result.ObjectID = item.Id;
            result.ParentID = item.ParentId;
            result.Class = new Platinum.ObjectClass("object.container.album.videoAlbum", "");
            result.Title = item.MbItem.Name;
            result.ChildrenCount = item.Children.Count();

            result.Description.Date = item.MbItem.PremiereDate.HasValue ? item.MbItem.PremiereDate.Value.ToString() : string.Empty;
            result.Description.Language = item.MbItem.Language == null ? string.Empty : item.MbItem.Language;
            result.Description.DescriptionText = "this is DescriptionText";
            result.Description.LongDescriptionText = item.MbItem.Overview == null ? string.Empty : item.MbItem.Overview;
            result.Description.Rating = item.MbItem.CommunityRating.ToString();

            result.Recorded.SeriesTitle = item.MBItem.Name;

            if (item.MbItem.Genres != null)
            {
                foreach (var genre in item.MbItem.Genres)
                {
                    result.Affiliation.AddGenre(genre);
                }
            }
            if (item.MbItem.People != null)
            {
                foreach (var person in item.MbItem.People)
                {
                    if (string.Equals(person.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase))
                        result.People.AddActor(new Platinum.PersonRole(person.Name, person.Role == null ? string.Empty : person.Role));
                    else if (string.Equals(person.Type, PersonType.MusicArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "MusicArtist"));
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "artist"));
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "Performer"));
                    }
                    else if (string.Equals(person.Type, PersonType.Composer, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Composer"));
                        result.Creator = person.Name;
                    }
                    else if (string.Equals(person.Type, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Writer"));
                    else if (string.Equals(person.Type, PersonType.Director, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Director"));
                        result.People.Director = result.People.Director + " " + person.Name;
                    }
                    else
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, person.Type == null ? string.Empty : person.Type));
                }
            }
            return result;
        }
        internal static Platinum.MediaContainer GetMediaObject(VideoFolderContainer item)
        {
            var result = new Platinum.MediaContainer();
            result.ObjectID = item.Id;
            result.ParentID = item.ParentId;
            result.Class = new Platinum.ObjectClass("object.container.storageFolder", "");
            result.Title = item.MbItem.Name;
            result.ChildrenCount = item.Children.Count();

            result.Description.Date = item.MbItem.PremiereDate.HasValue ? item.MbItem.PremiereDate.Value.ToString() : string.Empty;
            result.Description.Language = item.MbItem.Language == null ? string.Empty : item.MbItem.Language;
            result.Description.DescriptionText = "this is DescriptionText";
            result.Description.LongDescriptionText = item.MbItem.Overview == null ? string.Empty : item.MbItem.Overview;
            result.Description.Rating = item.MbItem.CommunityRating.ToString();

            result.Recorded.SeriesTitle = item.MBItem.Name;

            if (item.MbItem.Genres != null)
            {
                foreach (var genre in item.MbItem.Genres)
                {
                    result.Affiliation.AddGenre(genre);
                }
            }
            if (item.MbItem.People != null)
            {
                foreach (var person in item.MbItem.People)
                {
                    if (string.Equals(person.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase))
                        result.People.AddActor(new Platinum.PersonRole(person.Name, person.Role == null ? string.Empty : person.Role));
                    else if (string.Equals(person.Type, PersonType.MusicArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "MusicArtist"));
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "artist"));
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "Performer"));
                    }
                    else if (string.Equals(person.Type, PersonType.Composer, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Composer"));
                        result.Creator = person.Name;
                    }
                    else if (string.Equals(person.Type, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Writer"));
                    else if (string.Equals(person.Type, PersonType.Director, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Director"));
                        result.People.Director = result.People.Director + " " + person.Name;
                    }
                    else
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, person.Type == null ? string.Empty : person.Type));
                }
            }
            return result;
        }
        internal static Platinum.MediaContainer GetMediaObject(VideoGenreContainer item)
        {
            var result = new Platinum.MediaContainer();
            result.ObjectID = item.Id;
            result.ParentID = item.ParentId;
            result.Class = new Platinum.ObjectClass("object.container.genre.videoGenre", "");
            result.ChildrenCount = item.Children.Count();

            result.Title = item.MBItem.Name.EnsureNotNull();
            result.Description.DescriptionText = item.MBItem.Name.EnsureNotNull();
            result.Description.LongDescriptionText = item.MBItem.Name.EnsureNotNull();

            return result;
        }

        internal static Platinum.MediaContainer GetMediaObject(VideoActorContainer item)
        {
            var result = new Platinum.MediaContainer();
            result.ObjectID = item.Id;
            result.ParentID = item.ParentId;
            result.Class = new Platinum.ObjectClass("object.container", "");
            result.ChildrenCount = item.Children.Count();

            result.Title = item.MBItem.Name == null ? string.Empty : item.MBItem.Name;
            result.Description.DescriptionText = item.MBItem.Overview.EnsureNotNull();
            result.Description.LongDescriptionText = item.MBItem.Overview.EnsureNotNull();

            return result;
        }

        internal static Platinum.MediaItem GetMediaObject(VideoItem item, Platinum.DeviceSignature signature)
        {
            var result = new Platinum.MediaItem();

            result.ObjectID = item.Id.ToString();
            result.ParentID = item.ParentId;
            result.Title = item.MBItem.Name == null ? string.Empty : item.MBItem.Name;


            var episode = item.MBItem as MediaBrowser.Controller.Entities.TV.Episode;
            if (episode == null)
                result.Class = new Platinum.ObjectClass("object.item.videoItem", "");
            else
            {
                if (signature == Platinum.DeviceSignature.WMP)
                {
                    result.Class = new Platinum.ObjectClass("object.item.videoItem.videoBroadcast", "");

                    if (episode.Season != null)
                        result.Title = string.Format("{0}-{1}", episode.Season.Name, result.Title);
                    if (episode.Series != null)
                        result.Title = string.Format("{0}-{1}", episode.Series.Name, result.Title);

                    if (episode.IndexNumber.HasValue)
                        result.Recorded.EpisodeNumber = (uint)item.MBItem.IndexNumber.Value;
                    if (episode.Series != null)
                        result.Recorded.SeriesTitle = episode.Series.Name == null ? string.Empty : episode.Series.Name;

                    result.Recorded.ProgramTitle = episode.Name == null ? string.Empty : episode.Name;
                }
                else
                    result.Class = new Platinum.ObjectClass("object.item.videoItem", "");

            }
            result.Date = item.MBItem.ProductionYear.ToString();

            result.Description.Date = item.MBItem.PremiereDate.HasValue ? item.MBItem.PremiereDate.Value.ToString() : string.Empty;
            result.Description.Language = item.MBItem.Language == null ? string.Empty : item.MBItem.Language;
            result.Description.DescriptionText = item.MBItem.Overview == null ? string.Empty : item.MBItem.Overview;
            result.Description.LongDescriptionText = item.MBItem.Overview == null ? string.Empty : item.MBItem.Overview;
            result.Description.Rating = item.MBItem.CommunityRating.ToString();


            if (item.MBItem.Genres != null)
            {
                foreach (var genre in item.MBItem.Genres)
                {
                    result.Affiliation.AddGenre(genre);
                }
            }
            if (item.MBItem.People != null)
            {
                foreach (var person in item.MBItem.People)
                {
                    if (string.Equals(person.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase))
                        result.People.AddActor(new Platinum.PersonRole(person.Name, person.Role == null ? string.Empty : person.Role));
                    else if (string.Equals(person.Type, PersonType.MusicArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "MusicArtist"));
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "artist"));
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, "Performer"));
                    }
                    else if (string.Equals(person.Type, PersonType.Composer, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Composer"));
                        result.Creator = person.Name;
                    }
                    else if (string.Equals(person.Type, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Writer"));
                    else if (string.Equals(person.Type, PersonType.Director, StringComparison.OrdinalIgnoreCase))
                    {
                        result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Director"));
                        result.People.Director = result.People.Director + " " + person.Name;
                    }
                    else
                        result.People.AddArtist(new Platinum.PersonRole(person.Name, person.Type == null ? string.Empty : person.Type));
                }
            }

            //'restricted' attribute (true, false, 1, 0).
            //When restricted="true", the ability to change or delete the
            //Container or Person is restricted.            
            //result.Restricted

            return result;
        }


        internal static Platinum.MediaContainer GetMediaObject(MusicArtistContainer item)
        {
            var result = new Platinum.MediaContainer();
            result.ObjectID = item.Id;
            result.ParentID = item.ParentId;
            result.Class = new Platinum.ObjectClass("object.container.person.musicArtist", "");
            result.Title = item.MBItem.Name == null ? string.Empty : item.MBItem.Name;
            result.ChildrenCount = item.Children.Count();
            result.Description.DescriptionText = item.MBItem.Name == null ? string.Empty : item.MBItem.Name;
            result.Description.LongDescriptionText = item.MBItem.Name == null ? string.Empty : item.MBItem.Name;
            return result;
        }
        internal static Platinum.MediaContainer GetMediaObject(MusicAlbumContainer item)
        {
            var result = new Platinum.MediaContainer();
            result.ObjectID = item.Id;
            result.ParentID = item.ParentId;
            result.Class = new Platinum.ObjectClass("object.container.album.musicAlbum", "");
            result.Title = item.MBItem.Name == null ? string.Empty : item.MBItem.Name;
            result.ChildrenCount = item.Children.Count();
            result.Description.DescriptionText = item.MBItem.Name == null ? string.Empty : item.MBItem.Name;
            result.Description.LongDescriptionText = item.MBItem.Name == null ? string.Empty : item.MBItem.Name;
            return result;
        }
        internal static Platinum.MediaContainer GetMediaObject(MusicGenreContainer item)
        {
            var result = new Platinum.MediaContainer();
            result.ObjectID = item.Id;
            result.ParentID = item.ParentId;
            result.Class = new Platinum.ObjectClass("object.container.genre.musicGenre", "");
            result.ChildrenCount = item.Children.Count();
            result.Title = item.MBItem.Name.EnsureNotNull();
            result.Description.DescriptionText = item.MBItem.Name.EnsureNotNull();
            result.Description.LongDescriptionText = item.MBItem.Name.EnsureNotNull();
            return result;
        }

        internal static Platinum.MediaItem GetMediaObject(MusicItem item)
        {
            var result = new Platinum.MediaItem();

            result.ObjectID = item.Id.ToString();
            result.ParentID = item.ParentId;
            result.Title = item.MBItem.Name == null ? string.Empty : item.MBItem.Name;
            result.Class = new Platinum.ObjectClass("object.item.audioItem.musicTrack", "");
            result.Date = item.MBItem.ProductionYear.ToString();

            result.Description.Date = item.MBItem.PremiereDate.HasValue ? item.MBItem.PremiereDate.Value.ToString() : string.Empty;
            result.Description.Language = item.MBItem.Language == null ? string.Empty : item.MBItem.Language;
            result.Description.DescriptionText = item.MBItem.Overview == null ? string.Empty : item.MBItem.Overview;
            result.Description.LongDescriptionText = item.MBItem.Overview == null ? string.Empty : item.MBItem.Overview;
            result.Description.Rating = item.MBItem.CommunityRating.ToString();

            if (item.MBItem.Genres != null)
            {
                foreach (var genre in item.MBItem.Genres)
                {
                    result.Affiliation.AddGenre(genre);
                }
            }

            if (!string.IsNullOrWhiteSpace(item.MBItem.Artist))
                result.People.AddArtist(new Platinum.PersonRole(item.MBItem.Artist));
            result.People.Contributor = item.MBItem.AlbumArtist.EnsureNotNull();
            result.Affiliation.Album = item.MBItem.Album.EnsureNotNull();

            if (item.MBItem.People != null)
            {
                foreach (var person in item.MBItem.People)
                {
                    if (person.Name != null)
                    {
                        if (string.Equals(person.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase))
                            result.People.AddActor(new Platinum.PersonRole(person.Name, person.Role == null ? string.Empty : person.Role));
                        else if (string.Equals(person.Type, PersonType.MusicArtist, StringComparison.OrdinalIgnoreCase))
                        {
                            result.People.AddArtist(new Platinum.PersonRole(person.Name, "MusicArtist"));
                            result.People.AddArtist(new Platinum.PersonRole(person.Name, "artist"));
                            result.People.AddArtist(new Platinum.PersonRole(person.Name, "Performer"));
                        }
                        else if (string.Equals(person.Type, PersonType.Composer, StringComparison.OrdinalIgnoreCase))
                        {
                            result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Composer"));
                            result.Creator = person.Name;
                        }
                        else if (string.Equals(person.Type, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
                            result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Writer"));
                        else if (string.Equals(person.Type, PersonType.Director, StringComparison.OrdinalIgnoreCase))
                        {
                            result.People.AddAuthors(new Platinum.PersonRole(person.Name, "Director"));
                            result.People.Director = result.People.Director + " " + person.Name;
                        }
                        else
                            result.People.AddArtist(new Platinum.PersonRole(person.Name, person.Type == null ? string.Empty : person.Type));
                    }
                }
            }

            //'restricted' attribute (true, false, 1, 0).
            //When restricted="true", the ability to change or delete the
            //Container or Person is restricted.            
            //result.Restricted

            return result;
        }
    }
    internal static class PlatinumAlbumArtInfoHelper
    {
        internal static IEnumerable<Platinum.AlbumArtInfo> GetAlbumArtInfo(WellKnownContainerBase item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            return GetAlbumArtInfo(item.MbItem, context, urlPrefixes);
        }
        internal static IEnumerable<Platinum.AlbumArtInfo> GetAlbumArtInfo(VideoSeriesContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            return GetAlbumArtInfo(item.MbItem, context, urlPrefixes);
        }
        internal static IEnumerable<Platinum.AlbumArtInfo> GetAlbumArtInfo(VideoFolderContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            return GetAlbumArtInfo(item.MbItem, context, urlPrefixes);
        }

        internal static IEnumerable<Platinum.AlbumArtInfo> GetAlbumArtInfo(MusicItem item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            return GetAlbumArtInfo(item.MBItem, context, urlPrefixes);
        }
        internal static IEnumerable<Platinum.AlbumArtInfo> GetAlbumArtInfo(MusicArtistContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            return GetAlbumArtInfo(item.MBItem, context, urlPrefixes);
        }
        internal static IEnumerable<Platinum.AlbumArtInfo> GetAlbumArtInfo(MusicAlbumContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            return GetAlbumArtInfo(item.MBItem, context, urlPrefixes);
        }
        internal static IEnumerable<Platinum.AlbumArtInfo> GetAlbumArtInfo(VideoItem item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            return GetAlbumArtInfo(item.MBItem, context, urlPrefixes);
        }
        private static IEnumerable<Platinum.AlbumArtInfo> GetAlbumArtInfo(BaseItem item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.AlbumArtInfo>();
            if (item == null || item.Images == null)
                return result;

            //making the artwork a direct hit to the MediaBrowser server instead of via the DLNA plugin works for WMP
            //not sure it'll work for all other clients
            //Xbox360 Video App ignores it and askes for: video url + ?artwork=true
            foreach (var img in item.Images)
            {
                foreach (var prefix in urlPrefixes)
                {
                    result.Add(new Platinum.AlbumArtInfo(prefix + "Items/" + item.Id.ToString() + "/Images/" + img.Key));
                }
            }
            return result;
        }

        internal static IEnumerable<Platinum.AlbumArtInfo> GetAlbumArtInfo(VideoActorContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.AlbumArtInfo>();
            if (item.MBItem.Images != null)
            {
                foreach (var img in item.MBItem.Images)
                {
                    foreach (var prefix in urlPrefixes)
                    {
                        result.Add(new Platinum.AlbumArtInfo(new Uri(prefix + "Persons/" + item.MBItem.Name + "/Images/" + img.Key).ToString()));
                    }
                }
            }
            return result;
        }
        internal static IEnumerable<Platinum.AlbumArtInfo> GetAlbumArtInfo(VideoGenreContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.AlbumArtInfo>();
            if (item.MBItem.Images != null)
            {
                foreach (var img in item.MBItem.Images)
                {
                    foreach (var prefix in urlPrefixes)
                    {
                        result.Add(new Platinum.AlbumArtInfo(new Uri(prefix + "Genres/" + item.MBItem.Name + "/Images/" + img.Key).ToString()));
                    }
                }
            }
            return result;
        }
        internal static IEnumerable<Platinum.AlbumArtInfo> GetAlbumArtInfo(MusicGenreContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.AlbumArtInfo>();
            foreach (var prefix in urlPrefixes)
            {
                result.Add(new Platinum.AlbumArtInfo(prefix + "Genre/" + item.MBItem.Name + "/Images/Primary"));
            }
            return result;
        }

    }

    internal static class PlatinumMediaResourceHelper
    {
        internal static IEnumerable<Platinum.MediaResource> GetMediaResource(WellKnownContainerBase item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.MediaResource>();
            if (item == null)
                return result;

            //var resource = new Platinum.MediaResource();
            return result;
        }
        internal static IEnumerable<Platinum.MediaResource> GetMediaResource(VideoSeriesContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.MediaResource>();
            if (item == null)
                return result;

            //var resource = new Platinum.MediaResource();
            return result;
        }
        internal static IEnumerable<Platinum.MediaResource> GetMediaResource(VideoFolderContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.MediaResource>();
            if (item == null)
                return result;

            //var resource = new Platinum.MediaResource();
            return result;
        }
        internal static IEnumerable<Platinum.MediaResource> GetMediaResource(VideoActorContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.MediaResource>();
            if (item == null)
                return result;

            //var resource = new Platinum.MediaResource();
            return result;

            //var result = new List<Platinum.MediaResource>();
            //if (item == null)
            //    return result;

            //if (item.MBItem.Images != null)
            //{
            //    foreach (var img in item.MBItem.Images)
            //    {
            //        foreach (var prefix in urlPrefixes)
            //        {
            //            var resource = new Platinum.MediaResource();
            //            resource.ProtoInfo = Platinum.ProtocolInfo.GetProtocolInfoFromMimeType("image/jpeg", true, context);
            //            resource.URI = new Uri(prefix + "Persons/" + item.MBItem.Id.ToString() + "/Images/" + img.Key).ToString();

            //            result.Add(resource);
            //        }
            //    }
            //}
            //return result;
        }
        internal static IEnumerable<Platinum.MediaResource> GetMediaResource(VideoGenreContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.MediaResource>();
            if (item == null)
                return result;

            //var resource = new Platinum.MediaResource();
            return result;
        }

        internal static IEnumerable<Platinum.MediaResource> GetMediaResource(MusicGenreContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.MediaResource>();
            if (item == null)
                return result;

            //var resource = new Platinum.MediaResource();
            return result;
        }
        internal static IEnumerable<Platinum.MediaResource> GetMediaResource(MusicArtistContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.MediaResource>();
            if (item == null)
                return result;

            //var resource = new Platinum.MediaResource();
            return result;
        }
        internal static IEnumerable<Platinum.MediaResource> GetMediaResource(MusicAlbumContainer item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.MediaResource>();
            if (item == null)
                return result;

            //var resource = new Platinum.MediaResource();
            return result;
        }
    }
    internal static class MusicItemPlatinumMediaResourceHelper
    {
        internal static IEnumerable<Platinum.MediaResource> GetMediaResource(MusicItem item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.MediaResource>();
            if (item == null || item.MBItem == null)
                return result;

            var exts = ValidUriExtensions;
            foreach (var prefix in urlPrefixes)
            {
                foreach (var ext in exts)
                {
                    var resource = GetBasicMediaResource((BaseItem)item.MBItem);


                    //I'm unclear what /stream actaully returns
                    //but its sure hard to find a mime type for it
                    if (!string.IsNullOrWhiteSpace(ext))
                    {
                        var mimeType = MediaBrowser.Common.Net.MimeTypes.GetMimeType(ext);
                        resource.ProtoInfo = Platinum.ProtocolInfo.GetProtocolInfoFromMimeType(mimeType, true, context);
                    }

                    resource.URI = new Uri(prefix + "Audio/" + item.MBItem.Id.ToString() + "/stream" + ext).ToString();

                    result.Add(resource);
                }
            }

            return result;
        }


        private static Platinum.MediaResource GetBasicMediaResource(BaseItem item)
        {
            var result = new Platinum.MediaResource();

            //duration - The 'duration' attribute identifies the duration of the playback of
            //the resource, at normal speed.
            //The format of the duration string is:
            //H+:MM:SS[.F+], or H+:MM:SS[.F0/F1]
            //Where:
            //+H		one or more digits to indicate elapsed hours,
            //MM		exactly 2 digits to indicate minutes (00 to 59),
            //SS		exactly 2 digits to indicate seconds (00 to 59),
            //F+		any number of digits (including no digits) to indicate fractions of seconds,
            //F0/F1	a fraction, with F0 and F1 at least one digit long,
            //        and F0 < F1.
            //The string may be preceded by an optional + or - sign, and the
            //decimal point itself may be omitted if there are no fractional	seconds digits.            

            //we don't have to worry about the string formating because Platinum does it for us
            //we just have to give it the duration in seconds
            if (item.RunTimeTicks.HasValue)
                result.Duration = (uint)TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalSeconds;

            return result;
        }

        //private static IEnumerable<string> ValidUriExtensions
        //{
        //    get
        //    {
        //        return new List<string>() { ".flac", ".aac", ".wma", "", ".mp3", ".ogg" };
        //    }
        //}
        private static IEnumerable<string> ValidUriExtensions
        {
            get
            {
                return new List<string>() { ".mp3" };
            }
        }
    }
    internal static class VideoItemPlatinumMediaResourceHelper
    {
        internal static IEnumerable<Platinum.MediaResource> GetMediaResource(VideoItem item, Platinum.HttpRequestContext context, IEnumerable<string> urlPrefixes)
        {
            var result = new List<Platinum.MediaResource>();
            if (item == null || item.MBItem == null)
                return result;

            var videoOptions = GetTestVideoOptions(item);
            foreach (var prefix in urlPrefixes)
            {
                foreach (var opt in videoOptions)
                {
                    var resource = GetBasicMediaResource((BaseItem)item.MBItem);
                    //VideoBitRate - The bitrate in bytes/second of the resource.
                    resource.Bitrate = (uint)opt.VideoBitrate;

                    //ColourDepth - The color depth in bits of the resource (image or video).
                    //result.ColorDepth

                    //AudioChannels - Number of audio channels of the resource, e.g. 1 for mono, 2 for stereo, 6 for Dolby surround, etc.
                    resource.NbAudioChannels = (uint)opt.AudioChannels;

                    //Protection - Some statement of the protection type of the resource (not standardized).
                    //result.Protection

                    //Resolution - X*Y resolution of the resource (image or video).
                    //The string pattern is restricted to strings of the form:
                    //[0-9]+x[0-9]+
                    //(one or more digits,'x', followed by one or more digits).
                    //SampleFrequency - The sample frequency of the resource in Hz
                    //result.Resolution
                    resource.SampleFrequency = (uint)opt.SampleRate;

                    //Size - size, in bytes, of the resource.
                    //result.Size

                    //I'm unclear what /stream actaully returns
                    //but its sure hard to find a mime type for it
                    if (!string.IsNullOrWhiteSpace(opt.MimeExtension))
                    {
                        var mimeType = MediaBrowser.Common.Net.MimeTypes.GetMimeType(opt.MimeExtension);
                        resource.ProtoInfo = Platinum.ProtocolInfo.GetProtocolInfoFromMimeType(mimeType, true, context);
                    }

                    //http://25.62.100.208:8096/mediabrowser/Videos/7cb7f497-234f-05e3-64c0-926ff07d3fa6/stream.asf?audioChannels=2&audioBitrate=128000&videoBitrate=5000000&maxWidth=1920&maxHeight=1080&videoCodec=wmv&audioCodec=wma
                    //resource.URI = new Uri(prefix + "Videos/" + item.MBItem.Id.ToString() + "/stream" + opt.UriExtension).ToString();
                    var uri = string.Format("{0}Videos/{1}/stream{2}?audioChannels={3}&audioBitrate={4}&videoBitrate={5}&maxWidth={6}&maxHeight={7}&videoCodec={8}&audioCodec={9}",
                                            prefix, item.MBItem.Id, opt.UriExtension, opt.AudioChannels, opt.AudioBitrate, opt.VideoBitrate, opt.MaxWidth, opt.MaxHeight, opt.VideoCodec, opt.AudioCodec);
                    resource.URI = new Uri(uri).ToString();

                    result.Add(resource);
                }
            }

            return result;
        }


        ///// <summary>
        ///// returns a set of VideoOptions which all have their various BitRates, AudioChannels etc set to the same as the original file
        ///// </summary>
        ///// <param name="item"></param>
        ///// <returns></returns>
        //private static IEnumerable<VideoOptions> GetOriginalVideoOptions(VideoItem item)
        //{
        //    var result = new List<VideoOptions>();
        //    if (item == null || item.MBItem == null || item.MBItem.DefaultVideoStream == null)
        //        return result;

        //    var originalVideoOptions = new VideoOptions()
        //    {
        //        VideoBitrate = item.MBItem.DefaultVideoStream.BitRate.Value,
        //        AudioChannels = item.MBItem.DefaultVideoStream.Channels.Value,
        //        SampleRate = item.MBItem.DefaultVideoStream.SampleRate.Value
        //    };
        //    if (item.MBItem.Path != null && System.IO.Path.HasExtension(item.MBItem.Path))
        //    {
        //        originalVideoOptions.MimeExtension = System.IO.Path.GetExtension(item.MBItem.Path);
        //        originalVideoOptions.UriExtension = System.IO.Path.GetExtension(item.MBItem.Path);
        //    }
        //    //ensure that the uri extension is a valid uri extension
        //    if (ValidUriExtensions.Contains(originalVideoOptions.UriExtension))
        //        result.Add(originalVideoOptions);

        //    //add one of each valid uri extension
        //    foreach (var ext in ValidUriExtensions)
        //    {
        //        //skip original uri because it's already been added
        //        if (!string.Equals(ext, originalVideoOptions.UriExtension, StringComparison.OrdinalIgnoreCase))
        //        {
        //            var videoOptions = originalVideoOptions.Clone();
        //            videoOptions.MimeExtension = ext;
        //            videoOptions.UriExtension = ext;
        //            result.Add(videoOptions);
        //        }
        //    }
        //    return result;
        //}
        private static IEnumerable<VideoOptions> GetTestVideoOptions(VideoItem item)
        {
            var result = new List<VideoOptions>();
            if (item == null || item.MBItem == null || item.MBItem.DefaultVideoStream == null)
                return result;

            //audioChannels=2&audioBitrate=128000&videoBitrate=5000000&maxWidth=1920&maxHeight=1080&videoCodec=wmv&audioCodec=wma
            var originalVideoOptions = new VideoOptions()
            {
                AudioChannels = 2,
                AudioBitrate = 128000,
                VideoBitrate = 5000000,
                MaxWidth = 1920,
                MaxHeight = 1080,
            };

            //http://192.168.1.56:8096/mediabrowser/Videos/7cb7f497-234f-05e3-64c0-926ff07d3fa6/stream.asf?audioChannels=2&audioBitrate=128000&videoBitrate=5000000&maxWidth=1920&maxHeight=1080&videoCodec=h264&audioCodec=aac
            var asfOptions = originalVideoOptions.Clone();
            asfOptions.MimeExtension = ".asf";
            asfOptions.UriExtension = ".asf";
            asfOptions.VideoCodec = "h264";
            asfOptions.AudioCodec = "aac";
            result.Add(asfOptions);

            //audioChannels=2&audioBitrate=128000&videoBitrate=5000000&maxWidth=1920&maxHeight=1080&videoCodec=wmv&audioCodec=wma
            var wmvOptions = originalVideoOptions.Clone();
            wmvOptions.MimeExtension = ".wmv";
            wmvOptions.UriExtension = ".wmv";
            wmvOptions.VideoCodec = "wmv";
            wmvOptions.AudioCodec = "wma";
            result.Add(wmvOptions);

            //http://localhost:8096/mediabrowser/Videos/<id>/stream.webm?audioChannels=2&audioBitrate=128000&videoBitrate=5000000&maxWidth=1920&maxHeight=1080&videoCodec=vpx&audioCodec=Vorbis
            var webmOptions = originalVideoOptions.Clone();
            webmOptions.MimeExtension = ".webm";
            webmOptions.UriExtension = ".webm";
            webmOptions.VideoCodec = "vpx";
            webmOptions.AudioCodec = "vorbis";
            result.Add(webmOptions);

            ////http://localhost:8096/mediabrowser/Videos/<id>/stream.webm?audioChannels=2&audioBitrate=128000&videoBitrate=5000000&maxWidth=1920&maxHeight=1080&videoCodec=vpx&audioCodec=Vorbis
            //var webmMkvOptions = originalVideoOptions.Clone();
            //webmMkvOptions.MimeExtension = ".mkv";
            //webmMkvOptions.UriExtension = ".webm";
            //webmMkvOptions.VideoCodec = "vpx";
            //webmMkvOptions.AudioCodec = "vorbis";
            //result.Add(webmMkvOptions);

            ////http://192.168.1.56:8096/mediabrowser/Videos/7cb7f497-234f-05e3-64c0-926ff07d3fa6/stream.asf?audioChannels=2&audioBitrate=128000&videoBitrate=5000000&maxWidth=1920&maxHeight=1080&videoCodec=h264&audioCodec=aac
            //var mp4Options = originalVideoOptions.Clone();
            //mp4Options.MimeExtension = ".mp4";
            //mp4Options.UriExtension = ".mp4";
            //mp4Options.VideoCodec = "h264";
            //mp4Options.AudioCodec = "aac";
            //result.Add(mp4Options);

            return result;
        }

        private class VideoOptions
        {
            //audioChannels=2&audioBitrate=128000&videoBitrate=5000000&maxWidth=1920&maxHeight=1080&videoCodec=wmv&audioCodec=wma
            internal string MimeExtension { get; set; }
            internal string UriExtension { get; set; }
            internal int VideoBitrate { get; set; }
            internal string VideoCodec { get; set; }
            internal int MaxWidth { get; set; }
            internal int MaxHeight { get; set; }
            internal int SampleRate { get; set; }
            internal int AudioBitrate { get; set; }
            internal int AudioChannels { get; set; }
            internal string AudioCodec { get; set; }

            internal VideoOptions Clone()
            {
                return Clone(this);
            }
            internal static VideoOptions Clone(VideoOptions item)
            {
                return new VideoOptions()
                {
                    MimeExtension = item.MimeExtension,
                    UriExtension = item.UriExtension,
                    VideoBitrate = item.VideoBitrate,
                    VideoCodec = item.VideoCodec,
                    MaxWidth = item.MaxWidth,
                    MaxHeight = item.MaxHeight,
                    SampleRate = item.SampleRate,
                    AudioBitrate = item.AudioBitrate,
                    AudioChannels = item.AudioChannels,
                    AudioCodec = item.AudioCodec
                };
            }
        }

        private static Platinum.MediaResource GetBasicMediaResource(BaseItem item)
        {
            var result = new Platinum.MediaResource();

            //duration - The 'duration' attribute identifies the duration of the playback of
            //the resource, at normal speed.
            //The format of the duration string is:
            //H+:MM:SS[.F+], or H+:MM:SS[.F0/F1]
            //Where:
            //+H		one or more digits to indicate elapsed hours,
            //MM		exactly 2 digits to indicate minutes (00 to 59),
            //SS		exactly 2 digits to indicate seconds (00 to 59),
            //F+		any number of digits (including no digits) to indicate fractions of seconds,
            //F0/F1	a fraction, with F0 and F1 at least one digit long,
            //        and F0 < F1.
            //The string may be preceded by an optional + or - sign, and the
            //decimal point itself may be omitted if there are no fractional	seconds digits.            

            //we don't have to worry about the string formating because Platinum does it for us
            //we just have to give it the duration in seconds
            if (item.RunTimeTicks.HasValue)
                result.Duration = (uint)TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalSeconds;

            return result;
        }

        //private static IEnumerable<Platinum.MediaResource> GetSubtitleMediaResource(VideoItem item, Platinum.HttpRequestContext context)
        //{
        //    ////to do subtitles for clients that can deal with external subtitles (like srt)
        //    ////we will have to do something like this
        //    //IEnumerable<String> ips = GetUPnPIPAddresses(context);
        //    //foreach (var st in videoChild.MediaStreams)
        //    //{
        //    //    if (st.Type == MediaStreamType.Subtitle)
        //    //    {
        //    //        Platinum.MediaResource subtitleResource = new Platinum.MediaResource();
        //    //        subtitleResource.ProtoInfo = Platinum.ProtocolInfo.GetProtocolInfo(st.Path, with_dlna_extension: false);
        //    //        foreach (String ip in ips)
        //    //        {
        //    //            //we'll need to figure out which of these options works for whick players
        //    //            //either serve them ourselves
        //    //            resource.URI = new Uri("http://" + ip + ":" + context.LocalAddress.port + "/" + child.Id.ToString("D")).ToString();
        //    //            //or get the web api to serve them directly
        //    //            resource.URI = new Uri(Kernel.HttpServerUrlPrefix.Replace("+", ip) + "/api/video?id=" + child.Id.ToString() + "&type=Subtitle").ToString();
        //    //            result.AddResource(resource);
        //    //        }
        //    //    }
        //    //}
        //}




        //private static IEnumerable<string> ValidUriExtensions
        //{
        //    get
        //    {
        //        return new List<string>() { ".mkv", ".mpeg", ".avi", ".m4v", "", ".webm", ".asf", ".wmv", ".ogv", ".mp4", ".ts" };
        //    }
        //}


        //.mkv maps to a mime type of "video/x-matroska" in MB which Platinum will map to a protocol
        //.mpeg maps to a mime type of "video/mpeg" in MB which Platinum will map to a protocol
        //.avi maps to a mime type of "video/avi" in MB which Platinum will to map to a protocol
        //.asf maps to a mime type of "video/x-ms-asf" in MB which Platinum will to map to a protocol
        //.wmv maps to a mime type of "video/x-ms-wmv" in MB which Platinum will to map to a protocol
        //.mp4 maps to a mime type of "video/mp4" in MB which Platinum will to map to a protocol

        //.m4v maps to a mime type of "video/x-m4v" in MB which Platinum will NOT to map to a protocol
        //.webm maps to a mime type of "video/webm" in MB which Platinum will NOT to map to a protocol
        //.ogv maps to a mime type of "video/ogg" in MB which Platinum will NOT to map to a protocol
        //.ts maps to a mime type of "video/mp2t" in MB which Platinum will NOT map to a protocol

        private static IEnumerable<string> ValidUriExtensions
        {
            get
            {
                return new List<string>() { ".mkv", ".mpeg", ".avi", ".asf", ".wmv", ".mp4" };
            }
        }
    }


    internal static class StringExtensions
    {
        internal static string EnsureNotNull(this string item)
        {
            return item == null ? string.Empty : item;
        }
    }

    internal static class NavigationHelper
    {
        internal static Model.ModelBase GetObjectByID(User currentUser, string object_id)
        {
            var root = new Model.Root(currentUser);
            if (string.Equals(object_id, "0", StringComparison.OrdinalIgnoreCase))
                return root;
            else
                return root.GetChildRecursive(object_id);
        }
        internal static IEnumerable<Model.ModelBase> GetChildren(Model.ModelBase item, int startingIndex, int requestedCount)
        {
            //if they request zero children, they mean all children
            if (requestedCount == 0)
                return item.Children;
            else
                return item.GetChildren(startingIndex, requestedCount);
        }
        internal static IEnumerable<Model.ModelBase> GetRecursiveChildren(Model.ModelBase item, int startingIndex, int requestedCount)
        {
            //if they request zero children, they mean all children
            if (requestedCount == 0)
                return item.RecursiveChildren;
            else
                return item.GetChildrenRecursive(startingIndex, requestedCount);
        }
    }

    internal static class LibraryHelper
    {
        internal static MediaBrowser.Controller.Library.ILibraryManager LibraryManager { get; set; }

        internal static async Task<Person> GetPerson(PersonInfo personInfo)
        {
            return await LibraryManager.GetPerson(personInfo.Name);
        }
        internal static async Task<Genre> GetGenre(string name)
        {
            return await LibraryManager.GetGenre(name);
        }

    }
}
