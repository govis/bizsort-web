const API_BASE = process.env.NEXT_PUBLIC_API_URL || '';

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
  const payload = JSON.stringify(queryCopy);
  const response = await fetch(`${API_BASE}/api/product/profile/search?queryInput=${payload}`);
  
  if (!response.ok) {
    throw new Error(`Failed to perform product search: ${response.statusText}`);
  }
  
  return await response.json();
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
    if (response.status === 404) {
      // Temporary stub for missing backend endpoint
      console.warn("Product ToPreview endpoint not yet implemented on backend.");
      return products.map((p: any) => ({
        id: p.id || p,
        name: `Product ${p.id || p}`,
        text: "Placeholder product description until backend toPreview is implemented."
      }));
    }
    throw new Error(`Failed to fetch product previews: ${response.statusText}`);
  }
  
  return await response.json();
}
