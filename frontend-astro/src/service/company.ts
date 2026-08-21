import { apiFetch } from './api.js';
import type { CompanyPreview, SliceOutput, SearchItem } from '../components/types.js';
import { FetchOneCache, Cache, SessionCacheType } from '../session/cache';
import { Semantic } from '../model/foundation';



class CompanyProfileCache extends FetchOneCache<any> {
  get isTransient(): boolean {
    return true; // Match legacy: Do not store in sessionStorage
  }

  constructor() {
    super(SessionCacheType.CompanyProfile);
    this.isUserSpecific = false;
    this.enabled = false; // Phasing out frontend cache for models already cached in backend
    this.itemKey = 'id';
  }

  async fetch(key: number | string): Promise<any> {
    const response = await apiFetch('/api/company/profile/view?company=' + key);
    if (!response.ok && response.status === 404) return null;
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



export async function getOfferings(companyId: number, queryInput: any): Promise<any> {
    const response = await apiFetch('/api/company/profile/getOfferings?company=' + companyId + '&queryInput=' + JSON.stringify(queryInput));
    return response.json();
}

export async function toPreview(companies: any[]): Promise<any[]> {
    if (!companies || companies.length === 0) return [];
    const response = await apiFetch('/api/company/profile/toPreview?companies=' + encodeURIComponent(JSON.stringify(companies)));
    return response.json();
}

export async function getAffiliations(companyId: number, index: number, length: number): Promise<any> {
    const sliceInput = { index, length };
    const response = await apiFetch('/api/company/profile/getAffiliations?company=' + companyId + '&sliceInput=' + encodeURIComponent(JSON.stringify(sliceInput)));
    return response.json();
}

export async function search(queryInput: any): Promise<any> {
    const response = await apiFetch('/api/company/profile/search?queryInput=' + encodeURIComponent(JSON.stringify(queryInput)));
    return response.json();
}

export async function getFeatured(index: number, length: number, category: number = 0, location: number = 1): Promise<any> {
    const sliceInput = { index, length, category, location };
    const response = await apiFetch('/api/company/profile/getFeatured?sliceInput=' + encodeURIComponent(JSON.stringify(sliceInput)));
    return response.json();
}

export async function getCompanyFeaturedOfferings(companyId: number, index: number, length: number): Promise<any> {
    const sliceInput = { index, length };
    const response = await apiFetch('/api/company/offering/getFeatured?company=' + companyId + '&sliceInput=' + encodeURIComponent(JSON.stringify(sliceInput)));
    return response.json();
}

export async function getCommunities(companyId: number, index: number, length: number): Promise<any> {
    const sliceInput = { index, length };
    const response = await apiFetch('/api/company/profile/getCommunities?company=' + companyId + '&sliceInput=' + encodeURIComponent(JSON.stringify(sliceInput)));
    return response.json();
}
