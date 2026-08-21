import { apiFetch } from './api.js';
import type { SearchItem } from '../components/types.js';



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

export async function view(communityId: number): Promise<any> {
  if (!communityId) throw new Error('Community ID is required');
  const response = await apiFetch(`/api/community/profile/view?community=${communityId}`);
  if (!response.ok && response.status === 404) return null;
  return await response.json();
}
