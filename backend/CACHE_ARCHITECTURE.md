# BizSort Cache Architecture

The modernized caching layer is the backbone of the `BizSrt.Api` performance strategy. It perfectly ports the legacy C# caching algorithms while conforming to modern .NET 10 Minimal API dependency injection and singleton lifecycles.

## 1. Ownership & Process Boundary
- **Single Source of Truth**: The `LegacyCache` static class and all specific concrete caches (like `CompanyProfilesCache`, `SetsCache`, etc.) live **exclusively** inside the `BizSrt.Api` process.
- **Worker Isolation**: Background workers (`BizSrt.Worker`) **cannot** instantiate these caches. Workers execute timer sweeps against the database (EF Core) to find stale records, and then push commands via **gRPC** to the API. The API then updates the caches in-memory and triggers re-indexing calculations natively within its own process space.

## 2. Base Cache Types
All caches inherit from specialized base classes located in `BizSrt.Foundation.Cache`:
- `ReadManyExpirationCache<TKey, TValue>`: The primary read-through cache for dynamically loaded entities (like individual Company Profiles or Products). Items are fetched from EF Core upon cache miss and stored in a concurrent dictionary.
- `ReadAllCache<TKey, TValue>`: Used for smaller, global lookup tables (like Location Settings or Categories). The entire table is read into memory at application startup and refreshed periodically.
- `FolderItemCache` / `FeaturedCache`: Used for hierarchical or grouped data (like featured companies by location/category). These utilize dirty tracking via timestamps to invalidate specific hierarchical nodes when data changes.

## 3. Eviction Policy (LRU/LFU Hybrid)
Both the legacy system and the modern `BizSrt.Foundation.Cache` utilize `IExpirationItem` interfaces across cached models (e.g. `CachedValue`, `CachedSet`). 
- **HitCount**: Tracks total accesses.
- **LastHit**: A global auto-incrementing access stamp (not a DateTime, but a sequential counter) updated upon access.
- **Eviction**: During capacity cleanups, the Cache `Manager` evaluates `LastHit + HitCount` as a single unified score to safely identify and evict the lowest value (least popular + stalest) items without complex object-tracking graphs.

## 4. Lazy Loading & Side Effects (`SetsCache`)
Caches are not just passive data stores; they drive business logic. 
- In caches like `SetsCache` (for Company and Product Facet Sets), the `this[int setId]` indexer tracks usage analytics natively when accessed by the search UI.
- If a `SetsCache` experiences a cache miss, its `.Created()` virtual method intercepts the newly built cache object and kicks off a background task to fully rebuild the facet permutations via `IndexCompanyFacetSetAsync` or `IndexProductFacetSetAsync`.

## 5. Interface Collisions (`IKey<T>` vs EF Schema)
The legacy database occasionally uses columns named `Key` (e.g., `byte[] Key` in `CompanyFacetSet`). The caching layer expects models to implement `IKey<T>`, which demands a generic `T Key { get; }` property.
**Crucial Fix**: This creates a compiler collision. Always resolve this using Explicit Interface Implementation mapped to the Primary Key, and strictly decorate it with `[NotMapped]`.
```csharp
public int Id { get; set; }
public byte[] Key { get; set; } // SQL column
[NotMapped] int IKey<int>.Key => Id; // Cache Interface constraint
```
If `[NotMapped]` is omitted, EF Core 10's reflection engine will attempt to map the explicit interface to the database, causing startup crashes.

## 6. Dynamic Property Bags
Models like `CachedCompanyProfile` formerly used JSON serialization strings for dynamic properties. These have been upgraded to strongly typed `System.Text.Json.Serialization.JsonExtensionData` property bags (`Dictionary<string, object> Properties`). This natively integrates with the frontend Lit components while preserving memory efficiency.
