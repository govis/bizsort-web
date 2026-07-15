using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace BizSrt.Foundation.Cache
{
    public abstract class CachedNode<TKey, TMemberType> : PartCache, IKey<TKey>, IMemberType<TMemberType> where TKey : struct, IComparable
    {
        protected TKey zero = default(TKey);

        protected TKey _id;
        TKey IKey<TKey>.Key
        {
            get { return _id; }
        }
        protected string _name;
        protected TKey _parentKey;
        public TKey ParentKey
        {
            get { return _parentKey; }
        }

        public TKey Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Name
        {
            get { return _name; }
        }

        public byte SortOrder
        {
            get;
            set;
        }

        public abstract BizSrt.Model.Group.NodeType NodeType(TMemberType type);

        public abstract bool OfType(TMemberType type);

        public abstract CachedNode<TKey, TMemberType> Parent
        {
            get;
        }

        public abstract string DisplayText
        {
            get;
        }

        public abstract string DisplayPath
        {
            get;
        }

        public abstract BizSrt.Model.Group.Node<TKey> ParentNode(TMemberType type);

        public BizSrt.Model.Group.IdName<TKey> IdName
        {
            get
            {
                return new BizSrt.Model.Group.IdName<TKey> { Id = _id, Name = Name, NodeType = NodeType(default(TMemberType)) };
            }
        }

        public BizSrt.Model.Group.IdName<TKey>[] GetPath(BizSrt.Model.IEntityRef<TKey> root)
        {
            return GetPath(root, (cachedClass) => new BizSrt.Model.Group.IdName<TKey> 
            { 
                Id = cachedClass.Id, 
                Name = (cachedClass.Id.CompareTo(zero) > 0 ? cachedClass.Name : cachedClass.DisplayPath),
                NodeType = cachedClass.NodeType(default(TMemberType))
            });
        }

        public TMember[] GetPath<TMember>(BizSrt.Model.IEntityRef<TKey> root, Func<CachedNode<TKey, TMemberType>, TMember> populate)
            where TMember : BizSrt.Model.IEntityRef<TKey>
        {
            var cachedClass = this;
            var groups = new List<TMember>();
            do
            {
                groups.Insert(0, populate(cachedClass));
                if (root == null || cachedClass.Id.CompareTo(root.Id) > 0)
                    cachedClass = cachedClass.Parent;
                else
                    cachedClass = null;
            } while (cachedClass != null && (root != null || cachedClass.Id.CompareTo(zero) > 0));
            return groups.ToArray();
        }

        public static BizSrt.Model.Group.Node<TKey> PopulateWithChildren<TMember>(TKey classId, BizSrt.Model.Group.SubType type, TMemberType memberTypes, ITreeCache<TKey, TMember, TMemberType> cache)
            where TMember : CachedNode<TKey, TMemberType>, IKey<TKey>
        {
            var zero = default(TKey);
            var cachedClass = cache[classId];

            if ((type & BizSrt.Model.Group.SubType.Siblings) > 0 && classId.CompareTo(zero) > 0)
            {
                classId = cachedClass.ParentKey;
                cachedClass = cache[classId];
            }

            //var model = new BizSrt.Model.Group.Node<TKey> { Id = classId, Name = cachedClass.Name, NodeType = cachedClass.NodeType(memberTypes), Parent = (classId.CompareTo(zero) > 0 ? cachedClass.ParentModel(memberTypes) : null) };
            var model = cachedClass.ToNode(memberTypes);
            if (classId.CompareTo(zero) > 0)
                model.Parent = cachedClass.ParentNode(memberTypes);

            if ((type & BizSrt.Model.Group.SubType.Children) > 0 && cache.HasChildren(classId, memberTypes))
            {
                BizSrt.Model.Group.Node<TKey> childClass;
                TMember[] children;
                var classes = new List<BizSrt.Model.Group.Node<TKey>>();
                foreach (var cachedChild in cache[classId, memberTypes])
                {
                    //childClass = new BizSrt.Model.Group.Node<TKey> { Id = cachedChild.Id, Name = cachedChild.Name, NodeType = cachedChild.NodeType(memberTypes) };
                    childClass = cachedChild.ToNode(memberTypes);
                    childClass.Parent = model;
                    
                    if ((type & BizSrt.Model.Group.SubType.GrandChildren) > 0 && cache.HasChildren(cachedChild.Id, memberTypes))
                    {
                        children = cache[cachedChild.Id, memberTypes];
                        if (children != null && children.Length > 0)
                        {
                            //childClass.Children = children.Select(cc => new BizSrt.Model.Group.Node<TKey> { Id = cc.Id, Name = cc.Name, NodeType = cc.NodeType(memberTypes), HasChildren = cache.HasChildren(cc.Id, memberTypes), Parent = childClass }).ToArray();
                            childClass.Children = children.Select(cc => cc.ToNode(memberTypes)).ToArray();
                            foreach(var cc in childClass.Children)
                            {
                                cc.HasChildren = cache.HasChildren(cc.Id, memberTypes);
                                cc.Parent = childClass;
                            }
                            childClass.HasChildren = true;
                        }
                    }
                    else
                        childClass.HasChildren = cache.HasChildren(childClass.Id, memberTypes);

                    classes.Add(childClass);
                }
                model.Children = classes.ToArray();
            }

            return model;
        }

        public virtual BizSrt.Model.Group.Node<TKey> ToNode(TMemberType type)
        {
            return new BizSrt.Model.Group.Node<TKey> { Id = Id, Name = Name, NodeType = NodeType(type) };
        }

        public T ToModel<T>(BizSrt.Model.Group.DisplayType type) where T : BizSrt.Model.IEntityRef<TKey>, new()
        {
            T model = new T() { Id = Id, Name = Name };
            switch (type)
            {
                //case global::BizSrt.Model.Group.DisplayType.Name:
                //    model.Name = Name;
                //    break;
                case BizSrt.Model.Group.DisplayType.Text:
                    model.Name = DisplayText;
                    break;
                case BizSrt.Model.Group.DisplayType.Path:
                    model.Name = DisplayPath;
                    break;
            }

            return model;
        }
    }

    public abstract class CachedNode<T> : CachedNode<T, byte> where T : struct, IComparable
    {
        protected byte _type;

        public byte Type
        {
            get { return _type; }
        }

        public override bool OfType(byte type)
        {
            return ((_type & type) > 0);
        }

        public TMember[] GetPath<TMember>(BizSrt.Model.IEntityRef<T> root, Func<CachedNode<T>, TMember> populate)
            where TMember : BizSrt.Model.IEntityRef<T>
        {
            //return base.GetPath<TMember>(includeRoot, populate);
            //http://msdn.microsoft.com/en-us/library/dd799517(v=vs.110).aspx
            return base.GetPath(root, cachedClass => populate((CachedNode<T>)cachedClass));
        }

        public TMember EntityRef<TMember>(BizSrt.Model.Group.DisplayType type, Action<CachedNode<T>, TMember> populate)
            where TMember : BizSrt.Model.IEntityRef<T>, new()
        {
            var model = ToModel<TMember>(type);
            populate?.Invoke(this, model);
            return model;
        }
    }

    public interface ITreeCache<TMemberKey, TMember, TMemberType>
    {
        TMember this[TMemberKey key]
        {
            get;
        }

        TMember[] this[TMemberKey classKey, TMemberType memberTypes]
        {
            get;
        }

        bool HasChildren(TMemberKey classKey, TMemberType memberTypes);
    }

    public class TreeCache<TKey, TAltKey, TMember, TMemberType, TModel> : GroupCache<TKey, TKey, TAltKey, TMember, TMemberType, TModel>, ITreeCache<TKey, TMember, TMemberType>
        where TKey : struct, IComparable
        where TMember : CachedNode<TKey, TMemberType>
        where TMemberType : IComparable
        where TModel : DbContext
    {
        public TreeCache(FetchGroupMembers<TMember, TKey> fetchClassMethod, FetchOne<TMember, TKey> fetchOneMethod, FetchKey<TKey, TAltKey> fetchKeyMethod, Func<TModel, TAltKey, object, IKey<TKey>> insertMethod, Func<TModel> modelMethod)
            : base(fetchClassMethod, fetchOneMethod, fetchKeyMethod, insertMethod, modelMethod)
        {
        }

        public bool HasChildren(TKey classKey, TMemberType memberTypes)
        {
            if (memberTypes.CompareTo(anyMemberType) != 0) //throw new NotImplementedException();
                Console.WriteLine("Foundation.Cache.TreeCache.HasChildren(TKey, TMemberType): memberTypes={0}", memberTypes);

            var cachedMembers = this[classKey, memberTypes];
            return cachedMembers != null && cachedMembers.Length > 0;
        }

        //Borowed from FolderCache<TOwner, TFolderKey, TFolder>.GetChildren<T>
        public T[] GetChildren<T>(TKey parentKey, TKey lookupKey, TMemberType memberTypes, Func<TMember, T> populate, ITreeCache<TKey, TMember, TMemberType> cache)
            where T : BizSrt.Model.Group.NodeRef<TKey>
        {
            var folders = cache[parentKey, memberTypes].Select(populate).ToArray();
            if (lookupKey.CompareTo(default(TKey)) > 0)
            {
                var lookupFolder = folders.SingleOrDefault(f => f.Id.CompareTo(lookupKey) == 0);
                if (lookupFolder == null)
                {
                    //Added support for nested folder lookup
                    //T lookupFolder;
                    var lookupFolders = folders;
                    var folderPath = cache[lookupKey].GetPath(null);
                    for (var i = 0; i < folderPath.Length; i++)
                    {
                        lookupFolder = lookupFolders.SingleOrDefault(f => f.Id.CompareTo(folderPath[i].Id) == 0);
                        if (lookupFolder != null && lookupFolder.HasChildren)
                        {
                            lookupFolders = populateChildren(lookupFolder, memberTypes, populate, cache);
                            continue;
                        }
                        break;
                    }
                }
                else if (lookupFolder.HasChildren)
                    populateChildren(lookupFolder, memberTypes, populate, cache);
            }
            return folders;
        }

        T[] populateChildren<T>(T lookupFolder, TMemberType memberTypes, Func<TMember, T> populate, ITreeCache<TKey, TMember, TMemberType> cache)
            where T : BizSrt.Model.Group.NodeRef<TKey>
        {
            var lookupFolders = cache[lookupFolder.Id, memberTypes].Select(populate).ToArray();
            //Self referencing loop detected for property 'Parent' from Newtonsoft JsonSerializer
            //foreach (var lf in lookupFolders)
            //    lf.Parent = lookupFolder; 
            lookupFolder.Children = lookupFolders;
            return lookupFolders;
        }
    }
}
