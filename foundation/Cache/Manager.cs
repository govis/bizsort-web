using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BizSrt.Foundation.Cache
{
    public interface IKey<T>
    {
        T Key
        {
            get;
        }
    }

    public interface IExpirationItem
    {
        int HitCount
        {
            get;
            set;
        }

        int LastHit
        {
            get;
            set;
        }
    }

    internal class WebCacheTag : IWebCacheTag
    {
        protected const int TIME_INTERVAL = 60000;
        protected const int IDLE_CYCLES_BEFORE_RECALL = 10;
        protected const int WORK_CYCLES_BEFORE_RECALL = 5;
        
        //protected CancellationTokenSource _cancellation;
        protected DateTime _lastModified;

        internal WebCacheTag()
        {
            Reset();
        }

        public void Reset()
        {
            _lastModified = DateTime.Now;
        }

        public long ETag
        {
            get { return _lastModified.Ticks; }
        }

        public DateTime LastModified
        {
            get { return _lastModified; }
            set { _lastModified = value; }
        }
    }

    public interface IWebCacheTag
    {
        long ETag
        {
            get;
        }

        DateTime LastModified
        {
            get;
            set;
        }

        void Reset();
    }

    internal interface IOneCache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        int Count { get; }
        void Drop(TKey key);
        Action<TValue> ReflectHit { set; }
    }

    internal interface IManyCache<TKey, TValue> : IOneCache<TKey, TValue>
    {
        Action<TValue[]> ReflectHits { set; }
    }

    internal class Manager<TKey, TValue>
        where TValue : IExpirationItem
    {
        protected int _threshold;
        protected int _timeStamp = 0;
        protected TimeSpan TIME_INTERVAL;
        protected int IDLE_CYCLES_BEFORE_RECALL;
        protected int WORK_CYCLES_BEFORE_RECALL;

        IOneCache<TKey, TValue> _cache;

        internal Manager(IOneCache<TKey, TValue> cache, int threshold)
        {
            TIME_INTERVAL = TimeSpan.FromMinutes(1);
            //Disable RecallCycle
            IDLE_CYCLES_BEFORE_RECALL = int.MaxValue;
            WORK_CYCLES_BEFORE_RECALL = int.MaxValue;

            _cache = cache;
            _threshold = threshold;

            _cache.ReflectHit = (cachedValue) =>
            {
                if (cachedValue != null)
                {
                    cachedValue.LastHit = Interlocked.Increment(ref _timeStamp);
                    cachedValue.HitCount++; //Interlocked.Increment(ref cachedValue.HitCount);
                }
            };

            var manyCache = _cache as IManyCache<TKey, TValue>;
            if (manyCache != null)
            {
                manyCache.ReflectHits = (cachedValues) =>
                {
                    var lastHit = Interlocked.Increment(ref _timeStamp);
                    foreach (var cachedValue in cachedValues)
                    {
                        if (cachedValue != null)
                        {
                            cachedValue.LastHit = lastHit;
                            cachedValue.HitCount++; //Interlocked.Increment(ref cachedValue.HitCount);
                        }
                    }
                };
            }

            //Event.Log.Enqueue(new Event.Record(Settings.LogEventType.Information, "Foundation.Cache.Manager.Start", string.Format("Started cache manager for {0}", _cache.GetType().ToString())));
            //Start(); 
        }

        public void Stop(bool immediate)
        {
            //Event.Log.Enqueue(new Event.Record(Settings.LogEventType.Information, "Foundation.Cache.Manager.Stop", string.Format("Stopped cache manager for {0}", _cache.GetType().ToString())));
        }

        protected int PendingCount
        {
            get
            {
                return _cache.Count > _threshold ? (_cache.Count - _threshold) + _threshold / 3 : 0;
            }
        }

        protected bool WorkCycle(int pendingCount)
        {
            if (pendingCount > 0)
            {
                var q = from c in _cache
                        orderby c.Value.LastHit + c.Value.HitCount
                        select c.Key;
                foreach (var k in q.Take(pendingCount))
                    _cache.Drop(k);
                return true;
            }
            return false;
        }
    }
}
