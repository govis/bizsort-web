import type { SearchItem, SliceOutput } from '../components/types.js';
import { FetchOneCache, Cache, SessionCacheType } from '../session/cache';

const API_BASE = (typeof process !== 'undefined' && process.env.NEXT_PUBLIC_API_URL) 
  || (typeof import.meta !== 'undefined' && import.meta.env && import.meta.env.PUBLIC_API_URL)
  || 'https://localhost:5001';

/**
 * Ported legacy toPreview method.
 * Hydrates an array of SearchItems (which just contain IDs) into full Preview models.
 * Legacy backend method: Data.Community.Profile.ToPreview
 * Legacy frontend mapping: /community/profile/toPreview
 */
export async function toPreview(communities: SearchItem[]): Promise<any[]> {
  if (!communities || communities.length === 0) return [];
  
  const payload = JSON.stringify(communities);
  const response = await fetch(`${API_BASE}/api/community/profile/toPreview?communities=${payload}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch community previews: ${response.statusText}`);
  }
  
  return await response.json();
}

class CommunityProfileCache extends FetchOneCache<any> {
  get isTransient(): boolean {
    return true; // Match legacy: Do not store in sessionStorage
  }

  constructor() {
    super(SessionCacheType.CommunityProfile);
    this.isUserSpecific = false;
    this.itemKey = 'id';
  }

  async fetch(key: number | string): Promise<any> {
    const response = await fetch(`${API_BASE}/api/community/profile/view?community=${key}`);
    if (!response.ok) {
      throw new Error(`Failed to fetch community profile: ${response.statusText}`);
    }
    return await response.json();
  }
}

const communityCache = Cache.get(SessionCacheType.CommunityProfile, () => new CommunityProfileCache());

/**
 * Fetches a single community profile by its ID.
 * Matches legacy: view(community, options, ...)
 */
export async function view(communityId: number): Promise<any> {
  if (!communityId) throw new Error('Community ID is required');
  return communityCache.getItem(communityId);
}
