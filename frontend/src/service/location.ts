import { IdName, Autocomplete, Node, ResolvedLocation, SubType } from '../model/foundation';

const API_BASE = process.env.NEXT_PUBLIC_API_URL || '';

export async function autocomplete(parent: number, name: string, scope?: IdName): Promise<Autocomplete[]> {
    const scopeParam = scope ? `&scope=${encodeURIComponent(JSON.stringify(scope))}` : '';
    const response = await fetch(`${API_BASE}/api/location/autocomplete?parent=${parent}&name=${encodeURIComponent(name)}${scopeParam}`);
    
    if (!response.ok) {
        throw new Error(`Failed to autocomplete locations: ${response.statusText}`);
    }
    
    return await response.json();
}

export async function get(location: number, type?: number): Promise<any> {
    const typeParam = type !== undefined ? `&type=${type}` : '';
    const endpoint = type !== undefined ? 'get_Ref' : 'get';
    
    const response = await fetch(`${API_BASE}/api/location/${endpoint}?location=${location}${typeParam}`);
    
    if (!response.ok) {
        throw new Error(`Failed to fetch location: ${response.statusText}`);
    }
    
    return await response.json();
}

export async function populateChildren(parent: number, type: SubType, memberType: number): Promise<Node> {
    const response = await fetch(`${API_BASE}/api/location/populate_Children?parent=${parent}&type=${type}&memberType=${memberType}`);
    
    if (!response.ok) {
        throw new Error(`Failed to populate children: ${response.statusText}`);
    }
    
    return await response.json();
}

export async function getName(locationId: number): Promise<IdName> {
    const response = await fetch(`${API_BASE}/api/location/getName?locationId=${locationId}`);
    
    if (!response.ok) {
        throw new Error(`Failed to get location name: ${response.statusText}`);
    }
    
    return await response.json();
}

export async function populatePath(location: number, street?: number): Promise<any> {
    const endpoint = street !== undefined 
        ? `populate_Path_Street?city=${location}&street=${street}` 
        : `populate_Path?location=${location}`;
        
    const response = await fetch(`${API_BASE}/api/location/${endpoint}`);
    
    if (!response.ok) {
        throw new Error(`Failed to populate path: ${response.statusText}`);
    }
    
    return await response.json();
}

export async function resolve(city: string, street: string, allowCreate: boolean): Promise<ResolvedLocation> {
    const response = await fetch(`${API_BASE}/api/location/resolve`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ city, street, allowCreate })
    });
    
    if (!response.ok) {
        throw new Error(`Failed to resolve location: ${response.statusText}`);
    }
    
    const data = await response.json();
    return new ResolvedLocation(data);
}
