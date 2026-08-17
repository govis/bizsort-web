export enum SessionCacheType {
  CompanyProfile = 1,
  // Other cache types can be added here
}

export namespace Cache {
  const masterCache: Record<number, any> = {};

  export function get<T>(cacheType: SessionCacheType, factory: (type: SessionCacheType) => T): T {
    let cache = masterCache[cacheType];
    if (!cache) {
      cache = factory(cacheType);
      masterCache[cacheType] = cache;
    }
    return cache;
  }

  export function reset(full?: boolean) {
    for (const cacheType in masterCache) {
      const cache = masterCache[cacheType];
      if (cache.isUserSpecific || full) {
        cache.reset();
      }
    }
  }
}

export abstract class CacheBase<T> {
  isUserSpecific = false;
  isCompanySpecific = false;
  enabled = true;
  itemKey = 'id';
  
  protected items: T[];

  constructor(protected type: number, items: T[] = []) {
    this.items = items;
    if (typeof window !== 'undefined' && window.sessionStorage) {
      try {
        const cachedItems = sessionStorage.getItem(this.typeString);
        if (cachedItems) {
          if (!this.isTransient) {
            this.items = JSON.parse(cachedItems);
            return;
          } else {
            sessionStorage.removeItem(this.typeString);
          }
        }
      } catch (e) {
        console.error(e);
      }
    } else {
      // In SSR or when sessionStorage is unavailable, we just rely on in-memory items array
      // but disable persistence
      this.enabled = false;
    }
  }

  get typeString(): string {
    return this.type.toString();
  }

  get isTransient(): boolean {
    return false;
  }

  isDirty = false;

  save() {
    if (this.enabled && this.isDirty && !this.isTransient) {
      if (typeof window !== 'undefined' && window.sessionStorage) {
        sessionStorage.setItem(this.typeString, JSON.stringify(this.items));
      }
    }
  }

  reset() {
    if (this.enabled && typeof window !== 'undefined' && window.sessionStorage) {
      sessionStorage.removeItem(this.typeString);
    }
    if (this.items && this.items.length > 0) {
      this.items = [];
    }
    this.isDirty = false;
  }

  getItemInner(key: any): T | undefined {
    return this.items.find((item: any) => item[this.itemKey] == key);
  }
}

export abstract class FetchOneCache<T> extends CacheBase<T> {
  private fetchPromises: Record<string | number, Promise<T> | undefined> = {};

  abstract fetch(key: number | string): Promise<T>;

  constructor(type: number) {
    super(type, []);
  }

  /**
   * Modernized Promise-based getItem that automatically dedupes concurrent requests
   * for the same key, and falls back to fetching if missing.
   */
  async getItem(key: number | string): Promise<T> {
    const cachedItem = this.getItemInner(key);
    if (cachedItem !== undefined) {
      return cachedItem;
    }

    // Dedupe concurrent fetch requests for the same key
    if (this.fetchPromises[key]) {
      return this.fetchPromises[key];
    }

    const promise = this.fetch(key).then(data => {
      if (data && !this.getItemInner((data as any)[this.itemKey])) {
        this.items.push(data);
        this.isDirty = true;
      }
      delete this.fetchPromises[key];
      return data;
    }).catch(err => {
      delete this.fetchPromises[key];
      throw err;
    });

    this.fetchPromises[key] = promise;
    return promise;
  }
}
