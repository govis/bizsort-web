export interface INavigationOptions {
    suppressNavigate?: boolean;
    transition?: string;
    entityName?: string | string[];
    [key: string]: any;
}

export class Navigation {
    static go(path: string, params?: Record<string, any>, options?: INavigationOptions) {
        const searchParams = new URLSearchParams();
        if (params) {
            for (const key in params) {
                const value = params[key];
                if (value !== undefined && value !== null && value !== '') {
                    searchParams.append(key, typeof value === 'object' ? JSON.stringify(value) : value.toString());
                }
            }
        }
        const qs = searchParams.toString();
        const targetUrl = qs ? `${path}?${qs}` : path;
        
        window.dispatchEvent(new CustomEvent('app-navigate', {
            detail: { url: targetUrl, options }
        }));
    }
}

export namespace Company {
    export const homePage = "/companies";

    export function home(params?: Record<string, any>, options: INavigationOptions = {}) {
        if (!options.transition) options.transition = "Back";
        if (options.suppressNavigate) return { path: homePage, params: params || {} };
        return Navigation.go(homePage, params, options);
    }

    export const searchPage = "/companies/search";

    export function search(transactionType: number, category: number, query: string, location: number, near?: any, options?: INavigationOptions) {
        if (category > 0 || (query && query.trim() !== '')) {
            const params: any = {};
            if (transactionType) params.transactionType = transactionType;
            if (category > 0) params.categoryId = category;
            if (query) params.searchQuery = query;
            if (near && near.text) params.searchNear = near;
            else params.locationId = location;

            if (options && options.suppressNavigate) return { path: searchPage, params };
            return Navigation.go(searchPage, params, options);
        }
    }

    export function profileView(companyId: number, options: INavigationOptions = {}) {
        const path = `/company/${companyId}`;
        if (options.suppressNavigate) return { path, params: {} };
        return Navigation.go(path, {}, options);
    }

    export function tabView(companyId: number, tab: string, options: INavigationOptions = {}) {
        const path = `/company/${companyId}/${tab}`;
        if (options.suppressNavigate) return { path, params: {} };
        return Navigation.go(path, {}, options);
    }
}

export namespace Offering {
    export const homePage = "/offerings";

    export function home(params?: Record<string, any>, options: INavigationOptions = {}) {
        if (!options.transition) options.transition = "Back";
        if (options.suppressNavigate) return { path: homePage, params: params || {} };
        return Navigation.go(homePage, params, options);
    }

    export const searchPage = "/offerings/search";

    export function search(type: number, category: number, query: string, location: number, near?: any, options?: INavigationOptions) {
        if (category > 0 || (query && query.trim() !== '')) {
            const params: any = {};
            if (type) params.offeringType = type;
            if (category > 0) params.categoryId = category;
            if (query) params.searchQuery = query;
            if (near && near.text) params.searchNear = near;
            else params.locationId = location;

            if (options && options.suppressNavigate) return { path: searchPage, params };
            return Navigation.go(searchPage, params, options);
        }
    }

    export function profileView(offeringId: number, options: INavigationOptions = {}) {
        const path = `/offering/${offeringId}`;
        if (options.suppressNavigate) return { path, params: {} };
        return Navigation.go(path, {}, options);
    }

    export function view(accountId: number, offeringId: number, options: INavigationOptions = {}) {
        const path = `/company/${accountId}/offering/${offeringId}`;
        if (options.suppressNavigate) return { path, params: {} };
        return Navigation.go(path, {}, options);
    }
}
