# Session Progress Report

## 1. Astro SSR Bug Fixes (`company-offering`)
- **Node.js 18 TLS Rejection:** Identified that Node 18 native `fetch` entirely ignores the `NODE_TLS_REJECT_UNAUTHORIZED = '0'` flag during SSR.
- **Root Cause & Fix:** The `company-offering` page was rendering completely blank because the `fetch` to the backend failed, passing an empty object to the Lit UI. We resolved this by standardizing `API_BASE` to explicitly fallback to `http://localhost:5000` instead of `https://localhost:5001`.
- **Lit Hydration Logic:** Confirmed that when Lit evaluates `this.offering` against an empty object `{}`, it evaluates truthy, skipping the `!this.offering` early-return and proceeding to render empty HTML templates (hence the blank UI and "No description available" bug).

## 2. API Fetch Modernization (`apiFetch`)
- **Global Error Handling:** Implemented a unified `apiFetch()` wrapper in `frontend-astro\src\service\api.ts` to strictly replace all native inline `fetch()` calls.
- **Legacy Parity:** `apiFetch` successfully mimics the legacy `notifyErrorAjax` flow. It globally catches HTTP errors (excluding intentional `404 Not Found` returns for `view()` endpoints), parses backend JSON fault payloads (like `fault.Type`), and surfaces them to the user.
- **SSR-Safe Toasts:** Added `typeof document === 'undefined'` guard clauses so `apiFetch` handles API exceptions smoothly and safely during both Client-Side rendering and Astro Node.js Server-Side rendering without crashing.

## 3. WebAwesome v3 Toast Integration
- **Layout Integration:** Injected WebAwesome's native `<wa-toast id="app-toast" placement="bottom-end"></wa-toast>` Stack Manager component into the global `main.astro` layout.
- **Utility Method:** Created `src/components/utility/message-toast.ts` with a `showToast()` helper to securely present dynamic alerts anywhere in the application.

## 4. `ImagesCache` & The Legacy Image Service
- **Backend Modernization:** Fully ported the legacy `ImageService` to `.NET 10 Minimal APIs` at `/api/image/get`.
- **Enum Routing Fix:** Modified `backend\Data\Cache\ImageCache.cs` to explicitly map requests for `ImageEntity.Offering` (which maps to legacy enum `2` / `Product`) to correctly query `dbContext.OfferingMedia`. Previously, it only returned results for `ImageEntity.Company`.
- **Frontend Fallbacks:** Adjusted `frontend-astro\src\service\image.ts` `getLogoUrl` logic to properly fall back to `/images/bizsort-logo.svg` when image IDs are invalid/0. All UI sections (Header profile badges, Offering Cards, and Company Cards) now consistently show images!

## 5. Architectural Formalizations & Tracker Updates
- **Global `toPreview` Context:** Documented the critical architectural rule that `toPreview` hydration pipelines MUST map to Master/Global endpoints (e.g. `backend\Service\Offering\Profile.cs`) because the frontend passes naked global arrays of `{id}` identifiers, ignoring parent company scopes.
- **Layout Restructuring:** Renamed `Layout.astro` to `main.astro` to prepare for multi-layout architectures. Created a dedicated `crawler.astro` layout matching the legacy `crawler-main.ts` constraints (dropping heavy UI loading elements).
## 6. ListView Orchestration & SSR Modernization
- **Standalove SSR Page (`company/offerings.ts`):** Ported legacy `products.ts` logic into a dedicated Astro route (`/company/[id]/offerings`). Preserved the complex `View` model orchestration spanning `<list-header>`, `<list-pager>`, and `<offering-listview>`.
- **Lit ViewModel Host Orchestration:** Fixed a major regression where `CompanyOfferingsViewModel` failed to bind to its inner `listView` because the wrapper forced `listView: this as any`. Removing this override allowed `View.initialize()` to properly query the Shadow DOM and hydrate the list.
- **Lit Decorator Inheritance Bug:** Resolved a critical bug causing `<offering-slider>` to render blank data despite successful API calls. Discovered that defining `static get properties()` in a subclass completely wipes out the `@state()` reactivity of the base class (`ListSlider`). Refactored to purely use `@property()` decorators to ensure items update reactively.
- **`SliceOutput` vs `QueryOutput` Fix:** Uncovered that sliders were erroneously calling `getOfferings` (which returns a `totalCount`-based `QueryOutput`), breaking the infinite scroll `nextIndex`. Created `getCompanyFeaturedOfferings()` mapping to the correct legacy endpoint, strictly returning `SliceOutput` to restore slider pagination.
