using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;

namespace BizSrt.Api.Foundation.Cache
{
    [Flags]
    public enum ReadOneSuppress
    {
        None = 0,
        Fetch = 1,
        RecordNotFound = 2,
        Cache = 4
    }

    public delegate TValue FetchOne<TValue, TKey>(TKey key);

    public class ReadOneCache<TKey, TValue> : IOneCache<TKey, TValue>
    {
        protected object _manager;

        protected ConcurrentDictionary<TKey, TValue> _cache;
        protected FetchOne<TValue, TKey> _fetchOneMethod;

        public ReadOneCache(FetchOne<TValue, TKey> fetchOneMethod)
        {
            _cache = new ConcurrentDictionary<TKey, TValue>();
            _fetchOneMethod = fetchOneMethod;
        }

        public virtual TValue this[TKey key]
        {
            get
            {
                return this[key, ReadOneSuppress.None];
            }
        }

        public TValue this[TKey key, ReadOneSuppress suppress]
        {
            get
            {
                TValue cachedValue;
                if (!_cache.TryGetValue(key, out cachedValue) && (suppress & ReadOneSuppress.Fetch) == 0)
                {
                    cachedValue = _fetchOneMethod(key);
                    if (cachedValue != null && (suppress & ReadOneSuppress.Cache) == 0)
                        _cache.TryAdd(key, cachedValue);
                }

                if (cachedValue == null && (suppress & ReadOneSuppress.RecordNotFound) == 0)
                    throw new InvalidOperationException("Operation Failed");

                if (cachedValue != null && ReflectHit != null)
                    ReflectHit(cachedValue);
                
                return cachedValue;
            }
        }

        public bool Contains(TKey key)
        {
            return _cache.ContainsKey(key);
        }

        public void EnsureCached(TKey id)
        {
            TValue value;
            if (!_cache.TryGetValue(id, out value))
            {
                value = _fetchOneMethod(id);
                if (value != null)
                    _cache.TryAdd(id, value);
            }
        }

        public bool GetValue(TKey key, out TValue value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void Reset()
        {
            _cache.Clear();
        }

        public virtual void Drop(TKey key)
        {
            TValue value;
            _cache.TryRemove(key, out value);
        }

        public Action<TValue> ReflectHit
        {
            get;
            set;
        }

        int IOneCache<TKey, TValue>.Count
        {
            get
            {
                return _cache.Count;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _cache.GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return _cache.GetEnumerator();
        }
    }

    public class ReadOneExpirationCache<TKey, TValue> : ReadOneCache<TKey, TValue> where TValue : IExpirationItem
    {
        public ReadOneExpirationCache(FetchOne<TValue, TKey> fetchOneMethod, int threshold)
            : base(fetchOneMethod)
        {
            if (threshold > 0)
                _manager = new Manager<TKey, TValue>(this, threshold);
        }
    }
}
