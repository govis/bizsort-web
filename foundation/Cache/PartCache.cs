using System;
using System.Threading;


namespace BizSrt.Foundation.Cache
{
    public abstract class PartCache
    {
        protected T? Get<P, T>(ref T? part, P param, Func<P, T?> fetch) where T : class
        {
            var p = part;
            if (p == null)
            {
                p = fetch(param);
                Interlocked.CompareExchange(ref part, p, null);
            }

            return p;
        }

        protected T[]? GetArray<P, T>(ref T[]? part, P param, Func<P, T[]?> fetch)
        {
            return Get(ref part, param, fetch);
        }
    }
}
