import type { CompanyPreview, SliceOutput } from '../components/types.js';

const API_BASE = process.env.NEXT_PUBLIC_API_URL || '';

/**
 * Fetches a list of featured company entity IDs.
 * Matches legacy: getFeatured(sliceInput, ...)
 */
export async function getFeatured(index: number, length: number): Promise<SliceOutput<any>> {
  const sliceInput = JSON.stringify({ index, length });
  const response = await fetch(`${API_BASE}/api/company/profile/getFeatured?sliceInput=${encodeURIComponent(sliceInput)}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch featured companies: ${response.statusText}`);
  }
  
  return await response.json();
}

/**
 * Fetches preview details for a list of company entity IDs.
 * Matches legacy: toPreview(companies, options, ...)
 */
export async function toPreview(companies: any[]): Promise<CompanyPreview[]> {
  if (!companies || companies.length === 0) return [];
  
  const payload = JSON.stringify(companies);
  // Options is omitted for now as it's not strictly required in the modernized scaffolding
  const response = await fetch(`${API_BASE}/api/company/profile/toPreview?companies=${encodeURIComponent(payload)}`);
  
  if (!response.ok) {
    throw new Error(`Failed to fetch company previews: ${response.statusText}`);
  }
  
  return await response.json();
}
