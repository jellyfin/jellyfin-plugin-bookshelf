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
        internal ModelBase(User user, string id, string parentId)
        {
            this.User = user;
            this.Id = id;
            this.ParentId = parentId;
        }
        protected internal User User { get; private set; }
        protected internal string Id { get; private set; }
        protected internal string ParentId { get; private set; }
        protected internal abstract IEnumerable<ModelBase> Children { get; }
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
        protected internal ModelBase GetChildRecursive(string id)
        {
            return this.RecursiveChildren.FirstOrDefault(i => ((i != null) && (string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase))));
        }

        protected internal abstract Platinum.MediaObject MediaObject { get; }
        protected internal abstract Platinum.MediaResource MainMediaResource { get; }
        protected internal abstract string Extension { get; }

    }
    internal abstract class WellKnownContainerBase : ModelBase 
    {
        internal WellKnownContainerBase(User user, Folder mbFolder, string id, string parentId, string title)
            : base(user, id, parentId)
        {
            this.MbFolder = mbFolder;
            this.Title = title;
        }
        protected internal Folder MbFolder { get; private set; }
        protected internal string Title { get; private set; }
        internal Platinum.MediaContainer MediaContainer
        {
            get
            {
                var result = new Platinum.MediaContainer();
                result.ObjectID = this.Id;
                result.Class = new Platinum.ObjectClass("object.container", "");
                result.Title = this.Title;
                result.Description.DescriptionText = this.Title;
                result.Description.LongDescriptionText = this.Title;
                return result;
            }
        }
        protected internal override Platinum.MediaObject MediaObject
        {
            get { return this.MediaContainer; }
        }
        protected internal override Platinum.MediaResource MainMediaResource
        {
            get
            {
                var result = new Platinum.MediaResource();
                return result;
            }
        }
        protected internal override string Extension
        {
            get { return string.Empty; }
        }
    }
    internal class Root : WellKnownContainerBase
    {
        internal Root(User user)
            : base(user, user.RootFolder, "0", string.Empty, "Root")
        {
            this.Music = new MusicContainer(user);
            this.Video = new VideoContainer(user);
        }
        internal MusicContainer Music { get; private set; }
        internal VideoContainer Video { get; private set; }

        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return new List<ModelBase>() { this.Music, this.Video };
            }
        }
    }

    internal class MusicContainer : WellKnownContainerBase
    {
        internal MusicContainer(User user)
            : base(user, user.RootFolder, id: "1", parentId: "0", title: "Music")
        {
        }
        protected internal override Platinum.MediaObject MediaObject
        {
            get { return this.MediaContainer; }
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return new List<ModelBase>();
            }
        }
    }
    internal class VideoContainer : WellKnownContainerBase
    {
        internal VideoContainer(User user)
            : base(user, user.RootFolder, id: "2", parentId: "0", title: "Video")
        {
            this.AllVideo = new AllVideoContainer(user);
            this.Genre = new VideoFoldersContainer(user);
            this.Actor = new ActorContainer(user);
            this.Folders = new VideoFoldersContainer(user);
        }
        internal AllVideoContainer AllVideo { get; private set; }
        internal VideoFoldersContainer Genre { get; private set; }
        internal ActorContainer Actor { get; private set; }
        internal VideoFoldersContainer Folders { get; private set; }

        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return new List<ModelBase>() { this.AllVideo, this.Genre, this.Actor, this.Folders };
            }
        }
    }

    internal class AllVideoContainer : WellKnownContainerBase
    {
        internal AllVideoContainer(User user)
            : base(user, user.RootFolder, id: "8", parentId: "2", title: "All Video")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return this.MbFolder.GetRecursiveChildren(User).OfType<Video>().Select(i => new VideoItem(this.User, i, parentId: this.Id));
            }
        }
    }
    internal class ActorContainer : WellKnownContainerBase
    {
        internal ActorContainer(User user)
            : base(user, user.RootFolder, id: "A", parentId: "2", title: "Actors")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return this.MbFolder.GetRecursiveChildren(this.User)
                    .OfType<Video>()
                    .SelectMany(video =>
                    {
                        if (video.People == null)
                        {
                            return new PersonInfo[] { };
                        }
                        return video.People.Where(p => string.Equals(p.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(p.Name));
                    })
                    .DistinctBy(person => person.Name, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(person => person.Name)
                    .Select(i => new ActorItem(this.User, i, parentId: this.Id));
            }
        }

    }
    internal class VideoFoldersContainer : WellKnownContainerBase
    {
        internal VideoFoldersContainer(User user)
            : base(user, user.RootFolder, id: "15", parentId: "2", title: "Folders")
        {
        }
        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                var folderChildren = this.MbFolder.GetChildren(this.User).OfType<Folder>().Select(i => (ModelBase)(new VideoFolderContainer(this.User, i, this.Id)));
                var videoChildren = this.MbFolder.GetChildren(this.User).OfType<Video>().Select(i => (ModelBase)(new VideoItem(this.User, i, this.Id)));
                return folderChildren.Union(videoChildren);
            }
        }
    }

    internal class VideoFolderContainer : ModelBaseItem<Folder>
    {
        internal VideoFolderContainer(User user, Folder item, string parentId)
            : base(user, item, parentId)
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
        internal Platinum.MediaContainer MediaContainer
        {
            get
            {
                var result = new Platinum.MediaContainer();
                result.ObjectID = this.Id;
                result.Class = new Platinum.ObjectClass("object.container.storageFolder", "");
                result.Title = this.MBItem.Name;
                result.Description.DescriptionText = this.MBItem.Name;
                result.Description.LongDescriptionText = this.MBItem.Name;
                return result;
            }
        }
        protected internal override Platinum.MediaObject MediaObject
        {
            get { return this.MediaContainer; }
        }
        protected internal override Platinum.MediaResource MainMediaResource
        {
            get
            {
                var result = new Platinum.MediaResource();
                return result;
            }
        }
        protected internal override string Extension
        {
            get { return string.Empty; }
        }

    }


    internal abstract class ModelBaseItem<T> : ModelBase where T : BaseItem
    {
        internal ModelBaseItem(User user, T mbItem, string parentId)
            : base(user, id: mbItem.Id.ToString(), parentId: parentId)
        {
            this.MBItem = mbItem;
        }
        protected internal T MBItem { get; private set; }
    }

    internal class VideoItem : ModelBaseItem<Video>
    {
        internal VideoItem(User user, Video mbItem, string parentId)
            : base(user, mbItem, parentId)
        {

        }
        protected internal override IEnumerable<ModelBase> Children { get { return new List<ModelBase>(); } }
        protected internal override Platinum.MediaObject MediaObject
        {
            get { return this.MediaItem; }
        }
        internal Platinum.MediaItem MediaItem
        {
            get
            {
                var result = MediaItemHelper.GetMediaItem(this.MBItem);
                result.ParentID = this.ParentId;
                return result;
            }
        }
        protected internal override Platinum.MediaResource MainMediaResource
        {
            get
            {
                return MediaItemHelper.GetMediaResource(this.MBItem);
            }
        }
        protected internal override string Extension
        {
            get { return System.IO.Path.GetExtension(this.MBItem.Path); }
        }
    }

    internal class ActorItem : ModelBase
    {
        internal ActorItem(User user, PersonInfo item, string parentId)
            : base(user, item.Name.GetMD5().ToString(), parentId)
        {
            this.Item = item;
        }
        protected internal PersonInfo Item { get; private set; }

        protected internal override IEnumerable<ModelBase> Children
        {
            get
            {
                return this.User.RootFolder.GetRecursiveChildren(User)
                    .OfType<Video>()
                    .Where(i => i.People
                        .Any(p => string.Equals(p.Name, this.Item.Name, StringComparison.OrdinalIgnoreCase)))
                    .Select(i => new VideoItem(this.User, i, parentId: this.Id));
            }
        }

        protected internal override Platinum.MediaObject MediaObject
        {
            get { return this.MediaContainer; }
        }
        internal Platinum.MediaContainer MediaContainer
        {
            get
            {
                var result = new Platinum.MediaContainer();
                result.ObjectID = this.Id;
                result.ParentID = this.ParentId;
                result.Class = new Platinum.ObjectClass("object.container", "");

                result.Title = this.Item.Name == null ? string.Empty : this.Item.Name;
                result.Description.DescriptionText = string.Format("{0} {1} {2}", this.Item.Name, this.Item.Role, this.Item.Type);
                result.Description.LongDescriptionText = string.Format("{0} {1} {2}", this.Item.Name, this.Item.Role, this.Item.Type);

                return result;
            }
        }
        protected internal override Platinum.MediaResource MainMediaResource
        {
            get
            {
                var result = new Platinum.MediaResource();
                return result;
            }
        }
        protected internal override string Extension
        {
            get { return string.Empty; }
        }
    }
}
