using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BizSrt.Api.Foundation.Cache;

public abstract class ReadManyExpirationCache<TKey, TValue> where TKey : notnull
{
    private readonly Func<List<TKey>, Task<TValue[]>> _fetchMany;
    private readonly Func<TKey, Task<TValue?>> _fetchOne;
    private readonly Func<TValue, TKey> _getKey;

    protected readonly ConcurrentDictionary<TKey, TValue> _cache = new();

    protected ReadManyExpirationCache(
        Func<List<TKey>, Task<TValue[]>> fetchMany,
        Func<TKey, Task<TValue?>> fetchOne,
        Func<TValue, TKey> getKey)
    {
        _fetchMany = fetchMany;
        _fetchOne = fetchOne;
        _getKey = getKey;
    }

    public async Task<TValue[]> GetManyAsync(IEnumerable<TKey> keys)
    {
        var distinctKeys = keys.Distinct().ToList();
        var missingKeys = new List<TKey>();
        var results = new List<TValue>();

        foreach (var key in distinctKeys)
        {
            if (_cache.TryGetValue(key, out var val))
            {
                results.Add(val);
            }
            else
            {
                missingKeys.Add(key);
            }
        }

        if (missingKeys.Count > 0)
        {
            var fetched = await _fetchMany(missingKeys);
            foreach (var item in fetched)
            {
                var key = _getKey(item);
                _cache[key] = item;
                results.Add(item);
            }
        }

        return results.ToArray();
    }

    public async Task<TValue?> GetAsync(TKey key)
    {
        if (_cache.TryGetValue(key, out var val))
            return val;

        var fetched = await _fetchOne(key);
        if (fetched != null)
        {
            _cache[key] = fetched;
        }
        return fetched;
    }
}
