using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace PodCasts.Entities
{
    class VodCast : Folder
    {
        public string Url { get; set; }
        public IEnumerable<BaseItem> NonCachedChildren { get; set; }

        protected override IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService service)
        {
            return NonCachedChildren;
        }

        public override LocationType LocationType
        {
            get
            {
                return LocationType.Virtual;
            }
        }

        public async Task ValidatePodcastChildren()
        {

            if (NonCachedChildren == null) return; //nothing to validate

            //build a dictionary of the current children we have now by Id so we can compare quickly and easily
            var currentChildren = ActualChildren.ToDictionary(i => i.Id);

            //create a list for our validated children
            var validChildren = new ConcurrentBag<BaseItem>();
            var newItems = new ConcurrentBag<BaseItem>();

            var options = new ParallelOptions
                              {
                                  MaxDegreeOfParallelism = 20
                              };

            Parallel.ForEach(NonCachedChildren, options, child =>
                                                             {
                                                                 BaseItem currentChild;

                                                                 if (currentChildren.TryGetValue(child.Id, out currentChild))
                                                                 {
                                                                     //existing item - add and save if changed
                                                                     validChildren.Add(currentChild);
                                                                     if ((currentChild as IHasRemoteImage).HasChanged(child as IHasRemoteImage)) ServerEntryPoint.Instance.ItemRepository.SaveItem(child, CancellationToken.None).ConfigureAwait(false);

                                                                 }
                                                                 else
                                                                 {
                                                                     //brand new item - needs to be added
                                                                     newItems.Add(child);

                                                                     validChildren.Add(child);
                                                                 }
                                                             });

            // If any items were added or removed....
            if (!newItems.IsEmpty || currentChildren.Count != validChildren.Count)
            {
                var newChildren = validChildren;

                //that's all the new and changed ones - now see if there are any that are missing
                var itemsRemoved = currentChildren.Values.Except(newChildren).ToList();

                if (itemsRemoved.Any()) RemoveChildrenInternal(itemsRemoved);

                await LibraryManager.CreateItems(newItems, CancellationToken.None).ConfigureAwait(false);

                AddChildrenInternal(newItems);

                await ItemRepository.SaveChildren(Id, ActualChildren.Select(i => i.Id), CancellationToken.None).ConfigureAwait(false);

            }

        }

    }
}
