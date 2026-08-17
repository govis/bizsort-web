import { Semantic } from '../model/foundation';
import type { SliceOutput, SearchItem } from '../components/types.js';

const API_BASE = process.env.NEXT_PUBLIC_API_URL || '';

/**
 * Fetches a list of featured product entity IDs.
 * Matches legacy: getFeatured(sliceInput, ...)
 * Default: category=0 (any), location=1 (Canada)
 */
export async function getFeatured(index: number, length: number, category: number = 0, location: number = 1): Promise<SliceOutput<SearchItem>> {
  const sliceInput = JSON.stringify({ index, length, category, location });
  const response = await fetch(`${API_BASE}/api/product/profile/getFeatured?sliceInput=${sliceInput}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch featured products: ${response.statusText}`);
  }
  
  return await response.json();
}

/**
 * Ported legacy search method.
 * Fetches product search results including facets.
 * Legacy backend method: Data.Company.Product.Search
 * Legacy frontend mapping: /product/profile/search
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
  const payload = JSON.stringify(queryCopy);
  const response = await fetch(`${API_BASE}/api/product/profile/search?queryInput=${payload}`);
  
  if (!response.ok) {
    throw new Error(`Failed to perform product search: ${response.statusText}`);
  }
  
  const data = await response.json();
  // Mirror legacy: back-populate each FacetValue.name reference so the filter UI works
  if (data.facets) Semantic.Facet.deserialize(data.facets);
  return data;
}

/**
 * Ported legacy toPreview method.
 * Hydrates an array of SearchItems into full ProductPreview models.
 * Legacy backend method: Data.Company.Product.ToPreview
 * Legacy frontend mapping: /product/profile/toPreview
 */
export async function toPreview(products: any[]): Promise<any[]> {
  if (!products || products.length === 0) return [];
  
  const payload = JSON.stringify(products);
  const response = await fetch(`${API_BASE}/api/product/profile/toPreview?products=${payload}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch product previews: ${response.statusText}`);
  }
  
  return await response.json();
}
