import { showToast } from '../components/utility/message-toast.js';

export const API_BASE = import.meta.env.SSR ? 'http://localhost:5000' : '';

/**
 * A wrapper around native fetch that mimics the legacy notifyErrorAjax mechanism.
 * It automatically handles response checking, JSON parsing of error payloads,
 * and presenting toasts for UI feedback.
 */
export async function apiFetch(endpoint: string, options: RequestInit = {}): Promise<Response> {
    const url = endpoint.startsWith('http') ? endpoint : `${API_BASE}${endpoint}`;
    
    try {
        const response = await fetch(url, options);
        
        if (!response.ok) {
            if (response.status === 404) {
                // Return immediately for 404s so the caller can return null instead of throwing.
                return response;
            }

            let errorMessage = `HTTP ${response.status}: ${response.statusText}`;
            
            // Attempt to parse backend JSON error payloads (e.g. legacy fault.Type)
            try {
                // clone() because the caller might also want to read the body
                const errorData = await response.clone().json();
                if (errorData && errorData.message) {
                    errorMessage = errorData.message;
                } else if (errorData && errorData.Type) {
                    errorMessage = `Backend Fault: ${errorData.Type}`;
                }
            } catch (e) {
                // Ignore JSON parse errors, stick to status text
            }
            
            showToast(errorMessage, 'danger');
            
            // Throw so the calling service function doesn't try to parse a bad response body
            throw new Error(errorMessage);
        }
        
        return response;
    } catch (err: any) {
        // Only toast if it's a network error that we didn't already toast
        if (err.message !== 'Failed to fetch' && !err.message.startsWith('HTTP ') && !err.message.startsWith('Backend Fault')) {
            showToast(err.message || 'Network request failed', 'danger');
        }
        throw err;
    }
}
