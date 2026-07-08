using System;
using System.Collections.Concurrent;
using BizSrt.Api.Foundation.Cache;
using BizSrt.Api.Data;
using BizSrt.Api.Data.Cache.Location;

namespace BizSrt.Api.Data.Cache.Featured;

public abstract class FeaturedCache<TItems> : FolderItemCache<Tuple<short, int>, TItems>
{
    private readonly ConcurrentDictionary<Tuple<short, int>, DateTime> _dirtyStamps;

    protected FeaturedCache()
    {
        _dirtyStamps = new ConcurrentDictionary<Tuple<short, int>, DateTime>();
    }

    internal virtual void MarkDirty(Tuple<short, int> folder)
    {
        DateTime dirtyStamp;

        Tuple<short, int> dirtyFolder;
        
        var category = LegacyCache.Categories[folder.Item1]; 
        //GetPath doesn't return the root node (Id = 0), threre shouldn't be an "Everywhere" cache
        var locations = LegacyCache.Locations[folder.Item2]?.GetPath(null); 

        if (locations == null) return;

        do
        {
            foreach (var l in locations)
            {
                dirtyFolder = new Tuple<short, int>(category.Id, l.Id);
                if (!_dirtyStamps.TryGetValue(dirtyFolder, out dirtyStamp))
                    _dirtyStamps.TryAdd(dirtyFolder, DateTime.Now);
            }
            category = (CachedCategory)category.Parent; 
        }
        while (category != null); 
    }

    public TItems this[Tuple<short, int> folder, bool checkDirty]
    {
        get
        {
            DateTime dirtyStamp;
            if (checkDirty && _dirtyStamps.TryGetValue(folder, out dirtyStamp) && dirtyStamp < DateTime.Now.AddMinutes(-10))
            {
                _dirtyStamps.TryRemove(folder, out dirtyStamp);
                TItems folderItems;
                if (_folderItems.TryRemove(folder, out folderItems))
                    return folderItems;
            }
            return base[folder];
        }
    }
}
