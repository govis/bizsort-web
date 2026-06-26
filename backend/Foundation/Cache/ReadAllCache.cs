using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System;

namespace BizSrt.Api.Foundation.Cache
{
    [Flags]
    public enum ReadAllSuppress
    {
        None = 0,
        RecordNotFound = 2
    }

    public delegate IEnumerable<KeyValuePair<TKey, TValue>> FetchAll<TKey, TValue>();

    public class ReadAllCache<TKey, TValue>
    {
        private ConcurrentDictionary<TKey, TValue> __cache;
        protected ConcurrentDictionary<TKey, TValue> _cache
        {
            get
            {
                if (__cache == null)
                {
                    var data = _fetchAllMethod();
                    var cache = new ConcurrentDictionary<TKey, TValue>(data);
                    Interlocked.CompareExchange(ref __cache, cache, null);
                }
                return __cache;
            }
        }

        public void Reset()
        {
            __cache = null;
        }

        protected FetchAll<TKey, TValue> _fetchAllMethod;

        public ReadAllCache(FetchAll<TKey, TValue> fetchAllMethod)
        {
            _fetchAllMethod = fetchAllMethod;
        }

        protected TValue this[TKey key]
        {
            get
            {
                return this[key, ReadAllSuppress.None];
            }
        }

        protected TValue this[TKey key, ReadAllSuppress suppress]
        {
            get
            {
                TValue cachedValue;
                if (_cache.TryGetValue(key, out cachedValue) && cachedValue != null)
                    return cachedValue;
                else if ((suppress & ReadAllSuppress.RecordNotFound) == 0)
                    throw new InvalidOperationException("Operation Failed");

                return cachedValue;
            }
        }
    }
}
