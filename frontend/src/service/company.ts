import type { CompanyPreview, SliceOutput, SearchItem } from '../components/types.js';
import { FetchOneCache, Cache, SessionCacheType } from '../session/cache';

const API_BASE = process.env.NEXT_PUBLIC_API_URL || '';

/**
 * Fetches a list of featured company entity IDs.
 * Matches legacy: getFeatured(sliceInput, ...)
 */
export async function search(queryInput: any): Promise<any> {
  const queryCopy = { ...queryInput };
  if (queryCopy.searchQuery) {
    // Only encode the user-provided string to avoid HTTP parser truncation (e.g. '&', '=')
    queryCopy.searchQuery = encodeURIComponent(queryCopy.searchQuery);
  }
  const payload = JSON.stringify(queryCopy);
  const response = await fetch(`${API_BASE}/api/company/profile/search?queryInput=${payload}`);
  
  if (!response.ok) {
    throw new Error(`Failed to perform search: ${response.statusText}`);
  }
  
  return await response.json();
}

/**
 * Fetches a list of featured company entity IDs.
 * Matches legacy: getFeatured(sliceInput, ...) where sliceInput is List.DirectorySliceInput
 * Default: category=0 (any), location=1 (Canada) per LocationSettings.country.id
 */
export async function getFeatured(index: number, length: number, category: number = 0, location: number = 1): Promise<SliceOutput<SearchItem>> {
  const sliceInput = JSON.stringify({ index, length, category, location });
  const response = await fetch(`${API_BASE}/api/company/profile/getFeatured?sliceInput=${sliceInput}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch featured companies: ${response.statusText}`);
  }
  
  return await response.json();
}

/**
 * Ported legacy toPreview method.
 * Hydrates an array of SearchItems (which just contain IDs) into full Preview models.
 * Legacy backend method: Data.Company.Profile.ToPreview
 * Legacy frontend mapping: /company/profile/toPreview
 */
export async function toPreview(companies: SearchItem[]): Promise<CompanyPreview[]> {
  if (!companies || companies.length === 0) return [];
  
  const payload = JSON.stringify(companies);
  const response = await fetch(`${API_BASE}/api/company/profile/toPreview?companies=${payload}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch company previews: ${response.statusText}`);
  }
  
  return await response.json();
}

class CompanyProfileCache extends FetchOneCache<any> {
  get isTransient(): boolean {
    return true; // Match legacy: Do not store in sessionStorage
  }

  constructor() {
    super(SessionCacheType.CompanyProfile);
    this.isUserSpecific = false;
    this.itemKey = 'id';
  }

  async fetch(key: number | string): Promise<any> {
    const response = await fetch(`${API_BASE}/api/company/profile/view?company=${key}`);
    if (!response.ok) {
      throw new Error(`Failed to fetch company profile: ${response.statusText}`);
    }
    return await response.json();
  }
}

const companyCache = Cache.get(SessionCacheType.CompanyProfile, () => new CompanyProfileCache());

/**
 * Fetches a single company profile by its ID.
 * Matches legacy: view(company, options, ...)
 */
export async function view(companyId: number): Promise<any> {
  if (!companyId) throw new Error('Company ID is required');
  return companyCache.getItem(companyId);
}
