using System;
using System.Collections.Generic;
using System.Linq;

namespace BizSrt.Foundation.Cache
{
    public delegate TValue[] FetchMany<TValue, TKey>(List<TKey> keys);

    public class ReadManyCache<TKey, TValue> : ReadOneCache<TKey, TValue>, IManyCache<TKey, TValue> where TValue : IKey<TKey>
    {
        protected FetchMany<TValue, TKey> _fetchManyMethod;

        public ReadManyCache(FetchMany<TValue, TKey> fetchManyMethod, FetchOne<TValue, TKey> fetchOneMethod)
            : base(fetchOneMethod)
        {
            _fetchManyMethod = fetchManyMethod;
        }

        public TValue[] this[TKey[] keys]
        {
            get
            {
                return this[keys, false];
            }
        }

        public TValue[] this[TKey[] keys, bool explicitOrder]
        {
            get
            {
                if (keys != null && keys.Length > 0)
                {
                    TValue[] values;
                    if (explicitOrder)
                    {
                        //Using Dictionary to maintain sequential order
                        var dictionary = new Dictionary<TKey, TValue>();
                        List<TKey> pending = new List<TKey>();
                        TValue value;
                        foreach (TKey key in keys)
                        {
                            if (_cache.TryGetValue(key, out value))
                                dictionary.Add(key, value);
                            else //if (!pending.Contains(key))
                            {
                                dictionary.Add(key, default(TValue));
                                pending.Add(key);
                            }
                        }

                        if (pending.Count > 0)
                        {
                            var newValues = _fetchManyMethod(pending);
                            foreach (var nv in newValues)
                            {
                                _cache.TryAdd(nv.Key, nv);
                                dictionary[nv.Key] = nv;
                            }
                        }

                        values = dictionary.Values.Where(v => v != null).ToArray();
                    }
                    else //unorderd - more lightweight
                    {
                        var list = new List<TValue>();
                        List<TKey> pending = new List<TKey>();
                        TValue value;
                        foreach (TKey key in keys)
                        {
                            if (_cache.TryGetValue(key, out value))
                                list.Add(value);
                            else //if (!pending.Contains(key))
                                pending.Add(key);
                        }

                        if (pending.Count > 0)
                        {
                            var newValues = _fetchManyMethod(pending);
                            foreach (var nv in newValues)
                            {
                                _cache.TryAdd(nv.Key, nv);
                                list.Add(nv);
                            }
                        }

                        values = list.ToArray();
                    }

                    ReflectHits?.Invoke(values);

                    return values;
                }
                else
                    return new TValue[] {};
            }
        }

        public Action<TValue[]> ReflectHits
        {
            get;
            set;
        }
    }

    public class ReadManyExpirationCache<TKey, TValue> : ReadManyCache<TKey, TValue> where TValue : IKey<TKey>, IExpirationItem
    {
        public ReadManyExpirationCache(FetchMany<TValue, TKey> fetchManyMethod, FetchOne<TValue, TKey> fetchOneMethod, int threshold)
            : base(fetchManyMethod, fetchOneMethod)
        {
            if (threshold > 0)
                _manager = new Manager<TKey, TValue>(this, threshold);
        }
    }
}
