using System;
using System.Collections;
using System.Collections.Generic;


namespace BizSrt.Foundation.Cache
{
    //can be used in LINQ sub-queries so they execute only once
    public class Enumerator<TKey, TValue> : IEnumerator<TValue> where TValue : IKey<TKey>
    {
        IEnumerator<TValue> inner;
        List<TKey> cache;
        internal TKey[] Cache
        {
            get { return cache.ToArray(); }
        }

        internal Enumerator(IEnumerator<TValue> inner)
        {
            this.inner = inner;
            cache = new List<TKey>();
        }

        TValue IEnumerator<TValue>.Current
        {
            get
            {
                TValue current = inner.Current;
                cache.Add(current.Key);
                return current;
            }
        }

        object IEnumerator.Current
        {
            get { throw new NotImplementedException(); }
        }

        void IDisposable.Dispose()
        {
            inner.Dispose();
        }

        bool IEnumerator.MoveNext()
        {
            return inner.MoveNext();
        }

        void IEnumerator.Reset()
        {
            inner.Reset();
            cache.Clear();
        }
    }
}
