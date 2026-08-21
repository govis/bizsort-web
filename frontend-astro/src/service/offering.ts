import { apiFetch } from './api.js';
import { Semantic } from '../model/foundation';
import type { SliceOutput, SearchItem } from '../components/types.js';



/**
 * Fetches a list of featured offering entity IDs.
 * Matches legacy: getFeatured(sliceInput, ...)
 * Default: category=0 (any), location=1 (Canada)
 */
export async function getFeatured(index: number, length: number, category: number = 0, location: number = 1): Promise<SliceOutput<SearchItem>> {
  const sliceInput = JSON.stringify({ index, length, category, location });
  const response = await apiFetch(`/api/offering/profile/getFeatured?sliceInput=${sliceInput}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch featured offerings: ${response.statusText}`);
  }
  
  return await response.json();
}

/**
 * Ported legacy search method.
 * Fetches offering search results including facets.
 * Legacy backend method: Data.Company.Offering.Search
 * Legacy frontend mapping: /offering/profile/search
 */
export async function search(queryInput: any): Promise<any> {
  const queryCopy = { ...queryInput };
  if (queryCopy.searchQuery) {
    // Only encode the user-provided string to avoid HTTP parser truncation (e.g. '&', '=')
    queryCopy.searchQuery = encodeURIComponent(queryCopy.searchQuery);
  }
  if (queryCopy.searchNear && queryCopy.searchNear.text) {
    queryCopy.searchNear.text = encodeURIComponent(queryCopy.searchNear.text);
  }
  const payload = encodeURIComponent(JSON.stringify(queryCopy));
  const response = await apiFetch(`/api/offering/profile/search?queryInput=${payload}`);
  
  if (!response.ok) {
    throw new Error(`Failed to perform offering search: ${response.statusText}`);
  }
  
  const data = await response.json();
  // Mirror legacy: back-populate each FacetValue.name reference so the filter UI works
  if (data.facets) Semantic.Facet.deserialize(data.facets);
  return data;
}

/**
 * Ported legacy toPreview method.
 * Hydrates an array of SearchItems into full OfferingPreview models.
 * Legacy backend method: Data.Company.Offering.ToPreview
 * Legacy frontend mapping: /offering/profile/toPreview
 */
export async function toPreview(offerings: any[], options?: any): Promise<any[]> {
  if (!offerings || offerings.length === 0) return [];
  
  const payload = encodeURIComponent(JSON.stringify(offerings));
  let url = `/api/offering/profile/toPreview?offerings=${payload}`;
  if (options) {
    url += `&options=${encodeURIComponent(JSON.stringify(options))}`;
  }
  
  const response = await apiFetch(url);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch offering previews: ${response.statusText}`);
  }
  
  return await response.json();
}

/**
 * Fetches a single offering profile.
 * Matches legacy: view(company, offering, options)
 */
export async function view(offeringId: number, companyId: number = 0, options: any = { company: 1 }): Promise<any> {
  const optionsStr = encodeURIComponent(JSON.stringify(options));
  const response = await apiFetch(`/api/company/offering/view?company=${companyId}&offering=${offeringId}&options=${optionsStr}`);
  if (!response.ok && response.status === 404) return null;
  return await response.json();
}

