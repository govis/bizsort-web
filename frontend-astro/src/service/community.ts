import { apiFetch } from './api.js';
import type { SearchItem } from '../components/types.js';
import { FetchOneCache, Cache, SessionCacheType } from '../session/cache';



/**
 * Ported legacy toPreview method.
 * Hydrates an array of SearchItems (which just contain IDs) into full Preview models.
 * Legacy backend method: Data.Community.Profile.ToPreview
 * Legacy frontend mapping: /community/profile/toPreview
 */
export async function toPreview(communities: SearchItem[]): Promise<any[]> {
  if (!communities || communities.length === 0) return [];
  
  const payload = JSON.stringify(communities);
  const response = await apiFetch(`/api/community/profile/toPreview?communities=${payload}`);
  
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
    this.enabled = false; // Phasing out frontend cache for models already cached in backend
    this.itemKey = 'id';
  }

  async fetch(key: number | string): Promise<any> {
    const response = await apiFetch(`/api/community/profile/view?community=${key}`);
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
