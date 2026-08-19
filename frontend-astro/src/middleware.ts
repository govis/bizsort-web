import { defineMiddleware } from 'astro:middleware';

export const onRequest = defineMiddleware(async (context, next) => {
    const { url, request, redirect, cookies } = context;

    // 1. Legacy Token Interception (?t=)
    // We intercept legacy token state in query strings to issue 301 redirects to modern semantic URLs
    if (url.searchParams.has('t')) {
        const tParam = url.searchParams.get('t');
        if (tParam) {
            try {
                // Parse legacy token JSON
                // The JSON is URI encoded (but we use searchParams.get so it's already decoded)
                const token = JSON.parse(tParam);
                
                // NavigationProperty enum values:
                // ACTION = 0, CATEGORY_ID = 1, LOCATION_ID = 2, ACCOUNT_TYPE = 3, ACCOUNT_ID = 4, PRODUCT_ID = 5, SEARCH_QUERY = 21, SEARCH_NEAR = 22
                
                const action = token["0"]; // NavigationProperty.ACTION
                let newUrl: URL | null = null;
                
                if (action === 2 || action === 11) { 
                    // Action.View (2) or CompaniesView (11)
                    const accountId = token["4"]; // ACCOUNT_ID
                    const offeringId = token["5"]; // PRODUCT_ID
                    
                    if (accountId && offeringId) {
                        newUrl = new URL(`/company/${accountId}/offering/${offeringId}`, url.origin);
                    } else if (offeringId) {
                        newUrl = new URL(`/offering/${offeringId}`, url.origin);
                    } else if (accountId) {
                        newUrl = new URL(`/company/${accountId}`, url.origin);
                    }
                } else if (action === 3) { 
                    // Action.Search (3)
                    newUrl = new URL('/company/search', url.origin);
                    const categoryId = token["1"];
                    const locationId = token["2"];
                    const searchQuery = token["21"];
                    const searchNear = token["22"];
                    
                    if (categoryId) newUrl.searchParams.set('categoryId', categoryId.toString());
                    if (locationId) newUrl.searchParams.set('locationId', locationId.toString());
                    if (searchQuery) newUrl.searchParams.set('searchQuery', searchQuery);
                    if (searchNear) {
                        // searchNear is often { 1: "Text", 2: lat, 3: lng }
                        const nearText = searchNear["1"];
                        if (nearText) newUrl.searchParams.set('searchNear', JSON.stringify({ text: nearText, lat: searchNear["2"], lng: searchNear["3"] }));
                    }
                }
                
                if (newUrl) {
                    return redirect(newUrl.toString(), 301);
                }
            } catch (e) {
                console.error("Failed to parse legacy token", e);
            }
        }
    }

    // 2. Auth Routing (_validateToken)
    // Check if the user is attempting to access a protected route
    // Add paths here that require authentication
    const protectedRoutes = ['/account', '/dashboard'];
    const isProtectedRoute = protectedRoutes.some(route => url.pathname.startsWith(route));

    if (isProtectedRoute) {
        // The legacy system stored the session token in cookies or localStorage.
        // We use Astro cookies for Edge middleware reading
        const sessionToken = cookies.get('BizSrt.User.Token')?.value;

        if (!sessionToken) {
            // Redirect unauthenticated users to the login page
            const loginUrl = new URL('/login', url.origin);
            loginUrl.searchParams.set('returnUrl', url.pathname);
            return redirect(loginUrl.toString(), 302);
        }
    }

    // Proceed to the requested route
    return next();
});
