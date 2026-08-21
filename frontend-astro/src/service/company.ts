import { apiFetch } from './api.js';
import type { CompanyPreview, SliceOutput, SearchItem } from '../components/types.js';
import { Semantic } from '../model/foundation';



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
  if (queryCopy.searchNear && queryCopy.searchNear.text) {
    queryCopy.searchNear.text = encodeURIComponent(queryCopy.searchNear.text);
  }
  const payload = encodeURIComponent(JSON.stringify(queryCopy));
  const response = await apiFetch(`/api/company/profile/search?queryInput=${payload}`);
  
  if (!response.ok) {
    throw new Error(`Failed to perform search: ${response.statusText}`);
  }
  
  const data = await response.json();
  // Mirror legacy: back-populate each FacetValue.name reference so the filter UI works
  if (data.facets) Semantic.Facet.deserialize(data.facets);
  return data;
}


/**
 * Fetches a list of featured company entity IDs.
 * Matches legacy: getFeatured(sliceInput, ...) where sliceInput is List.DirectorySliceInput
 * Default: category=0 (any), location=1 (Canada) per LocationSettings.country.id
 */
export async function getFeatured(index: number, length: number, category: number = 0, location: number = 1): Promise<SliceOutput<SearchItem>> {
  const sliceInput = JSON.stringify({ index, length, category, location });
  const response = await apiFetch(`/api/company/profile/getFeatured?sliceInput=${sliceInput}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch featured companies: ${response.statusText}`);
  }
  
  return await response.json();
}

/**
 * Fetches the list of offerings for a specific company.
 * Matches legacy: /api/company/profile/getProducts -> /api/company/profile/getOfferings
 */
export async function getOfferings(companyId: number, queryInput: any): Promise<any> {
  const payload = encodeURIComponent(JSON.stringify(queryInput));
  const response = await apiFetch(`/api/company/profile/getOfferings?company=${companyId}&queryInput=${payload}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch company offerings: ${response.statusText}`);
  }
  
  const data = await response.json();
  if (data.facets) {
    Semantic.Facet.deserialize(data.facets);
  }
  return data;
}

/**
 * Fetches featured offerings for a specific company.
 * Matches legacy: /company/product/getFeatured
 */
export async function getCompanyFeaturedOfferings(companyId: number, index: number, length: number): Promise<SliceOutput<any>> {
  const sliceInput = JSON.stringify({ index, length });
  const response = await apiFetch(`/api/company/offering/getFeatured?company=${companyId}&sliceInput=${sliceInput}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch company featured offerings: ${response.statusText}`);
  }
  
  return await response.json();
}

/**
 * Fetches affiliations for a specific company.
 * Matches legacy: /api/company/profile/getAffiliations
 */
export async function getAffiliations(companyId: number, index: number = 0, length: number = 10): Promise<SliceOutput<SearchItem>> {
  const sliceInput = JSON.stringify({ index, length });
  const response = await apiFetch(`/api/company/profile/getAffiliations?company=${companyId}&sliceInput=${sliceInput}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch affiliations: ${response.statusText}`);
  }
  
  return await response.json();
}

/**
 * Fetches communities for a specific company.
 * Matches legacy: /api/company/profile/getCommunities
 */
export async function getCommunities(companyId: number, index: number = 0, length: number = 10): Promise<any> {
  const sliceInput = JSON.stringify({ index, length });
  const response = await apiFetch(`/api/company/profile/getCommunities?company=${companyId}&sliceInput=${sliceInput}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch communities: ${response.statusText}`);
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
  const response = await apiFetch(`/api/company/profile/toPreview?companies=${payload}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch company previews: ${response.statusText}`);
  }
  
  return await response.json();
}

export async function view(companyId: number): Promise<any> {
  if (!companyId) throw new Error('Company ID is required');
  const response = await apiFetch(`/api/company/profile/view?company=${companyId}`);
  if (!response.ok && response.status === 404) return null;
  return await response.json();
}
