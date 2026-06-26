using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System;
using BizSrt.Api.Model;

namespace BizSrt.Api.Foundation.Cache
{
    public class CachedFolder<K>
    {
        public K Parent
        {
            get;
            set;
        }

        public K Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public bool HasChildren
        {
            get;
            set;
        }
    }

    public abstract class CachedFolder : CachedFolder<byte>
    {
    }

    public abstract class FolderCache<TFolderKey, TFolder>
        where TFolder : CachedFolder<TFolderKey>
        where TFolderKey : IComparable
    {
        TFolderKey zero = default(TFolderKey);

        private TFolder[] __cache;
        protected TFolder[] _cache
        {
            get
            {
                if (__cache == null)
                {
                    var cache = fetchMethod();

                    if (cache.All(n => n.Parent.CompareTo(zero) == 0))
                        _isFlat = true;

                    Interlocked.CompareExchange(ref __cache, cache, null);

                    foreach (var node in cache)
                        if (cache.Any(n => n.Parent.CompareTo(node.Id) == 0))
                            node.HasChildren = true;

                    if (!_isFlat)
                    {
                        //http://www.daniweb.com/forums/thread118395.html
                        //foreach (var node in nodes)
                        //    if (node.parent.CompareTo(zero) == 0)
                        //        populateChildren(node, nodes);
                    }
                }
                return __cache;
            }
        }
        bool _isFlat;
        protected abstract TFolder[] fetchMethod();

        public bool IsEmpty
        {
            get
            {
                return _cache.Length == 0;
            }
        }

        public TFolder this[TFolderKey key]
        {
            get
            {
                var value = _cache.SingleOrDefault(n => n.Id.CompareTo(key) == 0);
                if (value != null)
                    return value;
                else
                    throw new InvalidOperationException("Operation Failed");
            }
        }

        public T[] GetChildren<T>(TFolderKey parentKey, TFolderKey lookupKey, Func<TFolder, T> populate)
            where T : /*BizSrt.Api.Model.Group.NodeRef<TFolderKey>*/IEntityId<TFolderKey>, BizSrt.Api.Model.Group.IChildren<T>, new()
        {
            return GetChildren(parentKey, lookupKey, populate, populate);
        }

        //TreeCache<TKey, TAltKey, TMember, TMemberType, TModel>.GetChildren<T>
        public T[] GetChildren<T, TChild>(TFolderKey parentKey, TFolderKey lookupKey, Func<TFolder, T> populate, Func<TFolder, TChild> populateChild = null) 
            where T : /*BizSrt.Api.Model.Group.NodeRef<TFolderKey>*/IEntityId<TFolderKey>, BizSrt.Api.Model.Group.IChildren<TChild>, new()
            where TChild: IEntityId<TFolderKey>
        {
            if (_isFlat && parentKey.CompareTo(default(TFolderKey)) == 0)
                return _cache.Select(populate).ToArray();
            else if (!_isFlat)
            {
                var cachedFolders = _cache.Where(f => f.Parent.CompareTo(parentKey) == 0);
                var folders = cachedFolders.Select(populate).ToArray();
                if (lookupKey.CompareTo(default(TFolderKey)) > 0)
                {
                    var cachedFolder = cachedFolders.SingleOrDefault(f => f.Id.CompareTo(lookupKey) == 0);
                    if (cachedFolder == null)
                    {
                        //Added support for nested folder lookup
                        //T lookupFolder;
                        var lookupFolders = folders;
                        var folderPath = GetPath<BizSrt.Api.Model.Group.NodeRef<TFolderKey>>(lookupKey);
                        for (var i = 0; i < folderPath.Length; i++)
                        {
                            cachedFolder = cachedFolders.SingleOrDefault(f => f.Id.CompareTo(folderPath[i].Id) == 0);
                            if (cachedFolder != null && cachedFolder.HasChildren)
                            {
                                cachedFolders = populateChildren(lookupFolders.Single(f => f.Id.CompareTo(cachedFolder.Id) == 0), populateChild);
                                lookupFolders = cachedFolders.Select(populate).ToArray();
                                continue;
                            }
                            break;
                        }
                    } 
                    else if (cachedFolder.HasChildren)
                        populateChildren(folders.Single(f => f.Id.CompareTo(cachedFolder.Id) == 0), populateChild);
                }
                else if (lookupKey.CompareTo(default(TFolderKey)) < 0) {
                    foreach(var cachedFolder in cachedFolders)
                        populateChildren(folders.Single(f => f.Id.CompareTo(cachedFolder.Id) == 0), populateChild);
                }

                return folders;
            }
            else
                return null;
        }

        IEnumerable<TFolder> populateChildren<T, TChild>(T lookupFolder, Func<TFolder, TChild> populate)
            where T : /*BizSrt.Api.Model.Group.NodeRef<TFolderKey>*/IEntityId<TFolderKey>, BizSrt.Api.Model.Group.IChildren<TChild>
            where TChild : IEntityId<TFolderKey>
        {
            var lookupFolders = _cache.Where(f => f.Parent.CompareTo(lookupFolder.Id) == 0);
            //Self referencing loop detected for property 'Parent' from Newtonsoft JsonSerializer
            //foreach (var lf in lookupFolders)
            //    lf.Parent = lookupFolder; 
            lookupFolder.Children = lookupFolders.Select(populate).ToArray();
            return lookupFolders;
        }

        /*public T[] GetChildren<T>(TFolderKey parentKey, TFolderKey lookupKey, Func<TFolder, T> populate)
            where T : BizSrt.Api.Model.Group.NodeRef<TFolderKey>, new()
        {
            if (_isFlat && parentKey.CompareTo(default(TFolderKey)) == 0)
                return _cache.Select(populate).ToArray();
            else if (!_isFlat)
            {
                var folders = _cache.Where(f => f.Parent.CompareTo(parentKey) == 0).Select(populate).ToArray();
                if (lookupKey.CompareTo(default(TFolderKey)) > 0)
                {
                    var lookupFolder = folders.SingleOrDefault(f => f.Id.CompareTo(lookupKey) == 0);
                    if (lookupFolder == null)
                    {
                        //Added support for nested folder lookup
                        //T lookupFolder;
                        var lookupFolders = folders;
                        var folderPath = GetPath<T>(lookupKey);
                        for (var i = 0; i < folderPath.Length; i++)
                        {
                            lookupFolder = lookupFolders.SingleOrDefault(f => f.Id.CompareTo(folderPath[i].Id) == 0);
                            if (lookupFolder != null && lookupFolder.HasChildren)
                            {
                                lookupFolders = populateChildren(lookupFolder, populate);
                                continue;
                            }
                            break;
                        }
                    }
                    else if (lookupFolder.HasChildren)
                        populateChildren(lookupFolder, populate);
                }
                return folders;
            }
            else
                return null;
        }

        T[] populateChildren<T>(T lookupFolder, Func<TFolder, T> populate)
            where T : BizSrt.Api.Model.Group.NodeRef<TFolderKey>
        {
            var lookupFolders = _cache.Where(f => f.Parent.CompareTo(lookupFolder.Id) == 0).Select(populate).ToArray();
            //Self referencing loop detected for property 'Parent' from Newtonsoft JsonSerializer
            //foreach (var lf in lookupFolders)
            //    lf.Parent = lookupFolder; 
            lookupFolder.Children = lookupFolders;
            return lookupFolders;
        }*/

        protected T GetDisplayPath<T>(TFolderKey key) where T : BizSrt.Api.Model.IEntityRef<TFolderKey>, new()
        {
            TFolder node;
            var path = new StringBuilder();
            var parent = key;
            while (parent.CompareTo(zero) > 0)
            {
                node = _cache.SingleOrDefault(n => n.Id.CompareTo(parent) == 0);
                if (node != null)
                {
                    if (path.Length > 0)
                        path.Insert(0, "\\");
                    path.Insert(0, node.Name);
                    parent = node.Parent;
                }
                else
                    parent = zero;
            }
            return new T { Id = key, Name = path.ToString() };
        }

        protected T[] GetPath<T>(TFolderKey key) where T : BizSrt.Api.Model.Group.NodeRef<TFolderKey>, new()
        {
            var path = new List<T>();
            getPath(path, key);
            return path.ToArray();
        }

        void getPath<T>(List<T> path, TFolderKey key) where T : BizSrt.Api.Model.Group.NodeRef<TFolderKey>, new()
        {
            if (key.CompareTo(zero) > 0)
            {
                var node = _cache.SingleOrDefault(f => f.Id.CompareTo(key) == 0);
                if (node != null)
                {
                    getPath(path, node.Parent);
                    path.Add(new T { Id = node.Id, Name = node.Name, ParentId = node.Parent });
                }
            }
        }
    }

    public abstract class FolderItemCache<TFolderKey, TItems>
    {
        protected ConcurrentDictionary<TFolderKey, TItems> _folderItems;

        protected FolderItemCache()
        {
            _folderItems = new ConcurrentDictionary<TFolderKey, TItems>();
        }

        protected abstract TItems FetchItems(TFolderKey folder);

        public virtual TItems this[TFolderKey folder]
        {
            get
            {
                TItems folderItems;
                if (!_folderItems.TryGetValue(folder, out folderItems))
                {
                    folderItems = FetchItems(folder);
                    _folderItems.TryAdd(folder, folderItems);
                }
                return folderItems;
            }
        }
    }
}
