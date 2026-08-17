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
    export const homePage = "/";

    export function home(params?: Record<string, any>, options: INavigationOptions = {}) {
        if (!options.transition) options.transition = "Back";
        if (options.suppressNavigate) return { path: homePage, params: params || {} };
        return Navigation.go(homePage, params, options);
    }

    export const searchPage = "/company/search";

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
}

export namespace Product {
    export const homePage = "/product";

    export function home(params?: Record<string, any>, options: INavigationOptions = {}) {
        if (!options.transition) options.transition = "Back";
        if (options.suppressNavigate) return { path: homePage, params: params || {} };
        return Navigation.go(homePage, params, options);
    }

    export const searchPage = "/product/search";

    export function search(type: number, category: number, query: string, location: number, near?: any, options?: INavigationOptions) {
        if (category > 0 || (query && query.trim() !== '')) {
            const params: any = {};
            if (type) params.productType = type;
            if (category > 0) params.categoryId = category;
            if (query) params.searchQuery = query;
            if (near && near.text) params.searchNear = near;
            else params.locationId = location;

            if (options && options.suppressNavigate) return { path: searchPage, params };
            return Navigation.go(searchPage, params, options);
        }
    }

    export function profileView(productId: number, options: INavigationOptions = {}) {
        const path = `/product/${productId}`;
        if (options.suppressNavigate) return { path, params: {} };
        return Navigation.go(path, {}, options);
    }

    export function view(accountId: number, productId: number, options: INavigationOptions = {}) {
        const path = `/company/${accountId}/product/${productId}`;
        if (options.suppressNavigate) return { path, params: {} };
        return Navigation.go(path, {}, options);
    }
}
