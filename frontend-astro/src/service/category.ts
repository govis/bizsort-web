import { apiFetch } from './api.js';
import type { IdName, Autocomplete, Node, NodeRef, SubType } from '../model/foundation';



export async function autocomplete(parent: number, name: string, scope?: IdName): Promise<any> {
    const scopeParam = scope ? `&scope=${JSON.stringify(scope)}` : '';
    const response = await apiFetch(`/api/category/autocomplete?parent=${parent}&name=${encodeURIComponent(name)}${scopeParam}`);
    
    if (!response.ok) {
        throw new Error(`Failed to autocomplete categories: ${response.statusText}`);
    }
    
    return await response.json();
}

export async function get(category: number, type?: number): Promise<any> {
    const typeParam = type !== undefined ? `&type=${type}` : '';
    const endpoint = type !== undefined ? 'get_Ref' : 'get';
    
    const response = await apiFetch(`/api/category/${endpoint}?category=${category}${typeParam}`);
    
    if (!response.ok) {
        throw new Error(`Failed to fetch category: ${response.statusText}`);
    }
    
    return await response.json();
}

export async function getPath(category: number, scope?: IdName): Promise<IdName[]> {
    const scopeParam = scope ? `&scope=${JSON.stringify(scope)}` : '';
    const response = await apiFetch(`/api/category/getPath?category=${category}${scopeParam}`);
    
    if (!response.ok) {
        throw new Error(`Failed to fetch category path: ${response.statusText}`);
    }
    
    return await response.json();
}

export async function populateChildren(parent: number, type: SubType, memberType: number): Promise<Node> {
    const response = await apiFetch(`/api/category/populate_Children?parent=${parent}&type=${type}&memberType=${memberType}`);
    
    if (!response.ok) {
        throw new Error(`Failed to populate children: ${response.statusText}`);
    }
    
    return await response.json();
}

export async function getChildren(parentCategory: number, lookupCategory: number): Promise<NodeRef[]> {
    const response = await apiFetch(`/api/category/getChildren?parentCategory=${parentCategory}&lookupCategory=${lookupCategory}`);
    
    if (!response.ok) {
        throw new Error(`Failed to get children: ${response.statusText}`);
    }
    
    return await response.json();
}
