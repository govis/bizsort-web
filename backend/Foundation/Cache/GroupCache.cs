using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace BizSrt.Api.Foundation.Cache
{
    public interface IMemberType<T>
    {
        bool OfType(T type);
    }

    public delegate TMember[] FetchGroupMembers<TMember, TGroupKey>(TGroupKey groupKey);

    public class GroupCache<TGroupKey, TMemberKey, TMemberAltKey, TMember, TMemberType, TModel> : TwoKeyCache<TMemberKey, TMemberAltKey, TMember, TModel>
        where TMemberKey : IComparable
        where TMember : IKey<TMemberKey>, IMemberType<TMemberType>
        where TMemberType : IComparable
        where TModel : DbContext
    {
        
        protected ConcurrentDictionary<TGroupKey, TMemberKey[]> _groupCache;
        protected FetchGroupMembers<TMember, TGroupKey> _fetchGroupMethod;

        protected TMemberType anyMemberType = default(TMemberType);

        public GroupCache(FetchGroupMembers<TMember, TGroupKey> fetchGroupMethod, FetchOne<TMember, TMemberKey> fetchOneMethod, FetchKey<TMemberKey, TMemberAltKey> fetchKeyMethod, Func<TModel, TMemberAltKey, object, IKey<TMemberKey>> insertMethod, Func<TModel> modelMethod)
            : base(fetchOneMethod, fetchKeyMethod, insertMethod, modelMethod)
        {
            _groupCache = new ConcurrentDictionary<TGroupKey, TMemberKey[]>();
            _fetchGroupMethod = fetchGroupMethod;
        }

        public virtual TMember[] this[TGroupKey groupKey, TMemberType memberTypes]
        {
            get
            {
                if (memberTypes.CompareTo(anyMemberType) != 0) //throw new NotImplementedException();
                    Console.WriteLine("Foundation.Cache.GroupCache[TGroupKey, TMemberType]: memberTypes={0}", memberTypes);

                TMemberKey[] cachedMemberKeys;
                if (!_groupCache.TryGetValue(groupKey, out cachedMemberKeys))
                {
                    var groupMembers = _fetchGroupMethod(groupKey);

                    if (groupMembers != null && groupMembers.Length > 0)
                    {
                        _groupCache.TryAdd(groupKey, groupMembers.Select(gm => gm.Key).ToArray());

                        foreach (var gm in groupMembers)
                            if (!_cache.ContainsKey(gm.Key))
                                _cache.TryAdd(gm.Key, gm);

                        if (memberTypes.CompareTo(anyMemberType) == 0)
                            return groupMembers;
                        else
                            return groupMembers.Where(gm => gm.OfType(memberTypes)).ToArray();
                    }
                    else
                    {
                        _groupCache.TryAdd(groupKey, null);
                        return null; //new TMember[] { };
                    }
                }
                else if (cachedMemberKeys != null && cachedMemberKeys.Length > 0)
                {
                    var groupMembersList = new List<TMember>();
                    foreach (var gmk in cachedMemberKeys)
                        groupMembersList.Add(base[gmk]);

                    if (memberTypes.CompareTo(anyMemberType) == 0)
                        return groupMembersList.ToArray();
                    else
                        return groupMembersList.Where(gm => gm.OfType(memberTypes)).ToArray();
                }
                else
                    return null; //new TMember[] { };
            }
        }

        public void DropGroup(TGroupKey groupKey)
        {
            TMemberKey[] cachedMemberKeys;
            _groupCache.TryRemove(groupKey, out cachedMemberKeys);
        }
    }

    public class GroupSearchCache<TKey, TValue> : ReadOneCache<GroupSearchCache<TKey>, TValue[]> 
        where TKey : IComparable
    {
        public GroupSearchCache(FetchOne<TValue[], GroupSearchCache<TKey>> fetchOneMethod) : base(fetchOneMethod) { }
    }

    public struct GroupSearchCache<TKey> where TKey : IComparable
    {
        //public CategorySearchKey(TKey parent, string name)
        //{
        //    Parent = parent;
        //    Name = name;
        //}

        public TKey Parent
        {
            get;
            set;
        }

        string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    _name = value.ToLowerInvariant();
                else
                    throw new ArgumentException("Name");
            }
        }

        public override int GetHashCode()
        {
            return Parent.GetHashCode() ^ Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is GroupSearchCache<TKey>)
            {
                var key = (GroupSearchCache<TKey>)obj;
                return Parent.CompareTo(key.Parent) == 0 && Name == key.Name;
            }
            else
                return false;
        }
    }
}
