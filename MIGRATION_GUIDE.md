# Migration Guidelines: Legacy to Lit & Web Awesome

This document outlines the standardized workflow for porting legacy Polymer/Material components to the modernized **Next.js 16 + Lit 3.3 + Web Awesome** stack.

👉 **For a complete overview of the architectural paradigm shift (from the legacy SPA router to Next.js App Router), please read [SPA_MODERNIZATION.md](file:///C:/Bizsort/bizsort-web/frontend/SPA_MODERNIZATION.md).**

## 1. Architectural Mapping

| Legacy Concept | Modern Implementation | Notes |
| :--- | :--- | :--- |
| `PageElement` | `LitElement` | Use `willUpdate` for data fetching on property change. |
| `connect(store)` | Direct API fetching | Fetch via `NEXT_PUBLIC_API_URL` in component methods. |
| Material Web Components (`md-`) | Web Awesome (`wa-`) | Use `@awesome.me/webawesome` package. |
| `cssLayout` / `cssKeyframes` | Vanilla CSS in `static styles` | **Tailwind CSS is strictly forbidden.** |
| `company-header-layout` | `<company-header-layout>` | Custom Lit component with slots: `navbar`, `dropdown`, `logo`, `tabs`, default. |
| `tab-bar` / `navigation-tab` | `<wa-tab-group>` / `<wa-tab>` | Sync state via `@wa-tab-show`. |
| `layout-card` | `<layout-card>` | Custom Lit wrapper around `wa-card` with Material Design shadow. |
| `page-menu` | `<page-menu>` | 3-dot dropdown using `wa-dropdown`. Supports `theme="dark"` for white icons. |
| `search-box` | `<search-box>` | Uses `wa-input` with pill style. Styled for dark header backgrounds. |
| `search-category-menu` | `<search-category-menu>` | Category dropdown. Currently a stub — needs location-aware "in/near" search. |

## 2. Component Translation Table

When porting legacy `.ts` files from `..\legacy\website\wwwroot`, map elements as follows:

| Legacy / Material Element | Web Awesome Equivalent |
| :--- | :--- |
| `<mwc-select>` / `<company-offce-select>` | `<wa-select>` |
| `<mwc-list-item>` / `<md-list-item>` | `<wa-option>` (inside select) or `<wa-dropdown-item>` |
| `<md-icon>` | `<wa-icon>` (uses Bootstrap icon names, not Material) |
| `<md-icon-button>` | `<wa-button variant="text" is-icon-button>` |
| `<paper-card>` | `<wa-card>` or `<layout-card>` |
| `<mwc-dropdown>` / `<md-menu>` | `<wa-dropdown>` |
| `<mwc-button>` / `<md-button>` | `<wa-button>` |
| `<md-filled-text-field>` | `<wa-input>` |
| `<round-fab>` | `<wa-button variant="primary" is-icon-button>` |
| `<md-list>` | `div.info-list` with `div.info-item` rows |
| `<app-header-layout>` / `<app-header>` | Custom CSS layout (no Polymer app-layout) |
| `<richtext-view>` | `unsafeHTML(content)` from `lit/directives/unsafe-html.js` |
| `<tab-bar>` / `<navigation-tab>` | `<wa-tab-group>` / `<wa-tab>` |

### Icon Name Mapping (Material → Bootstrap Icons):
| Material Icon | Web Awesome (Bootstrap) Icon |
| :--- | :--- |
| `place` | `geo-alt` |
| `phone` | `telephone` |
| `print` | `printer` |
| `email` | `envelope` |
| `launch` | `box-arrow-up-right` |
| `folder` | `folder` |
| `phone_android` | `phone` |
| `search` | `search` |
| `more_vert` | `three-dots-vertical` |
| `map` | `map` |
| `arrow_drop_down` | `caret-down-fill` |

## 3. Data Integrity & JSON Parity

To avoid breaking ported logic, the .NET 10 backend must maintain exact JSON parity with legacy payloads.

### Guidelines:
1. **Nested DTOs**: Prefer nested structures (e.g., `company.location.address`) over flat properties if that's what the legacy system used.
2. **DTO Mapping**: Perform heavy lifting in the Backend Service (e.g., `CompanyService.cs`) rather than the frontend.
3. **Address Construction**: Join individual DB fields (StreetNumber, StreetName, etc.) into a single `Address` string in the DTO to match legacy frontend expectations.
4. **Head Office**: In legacy, `headOffice` is a getter returning `offices[0]`. The backend now returns it as a separate field in the `Profile` DTO for convenience.
5. **URL Payload Encoding**: The legacy API endpoints expect raw JSON strings in the query parameters (e.g. `?queryInput={"startIndex":0}`). To maintain readability in DevTools and avoid unreadable escape sequences (`%7B%22...`), do **not** use `encodeURIComponent` on the entire JSON string. Instead, use standard `JSON.stringify()`, but be sure to selectively `encodeURIComponent()` only the user-provided string fields (like `searchQuery`) *before* stringifying. This preserves literal JSON brackets while safely encoding characters like `&` or `=` that would otherwise truncate the HTTP request.

## 4. Frontend Component Structure

All modernized components should live in `frontend/src/components/`.

### Shared Types:
Define interfaces in `types.ts` and import them — do not duplicate interfaces in component files.

### Template Pattern:
```typescript
import { LitElement, html, css } from 'lit';
import type { MyData } from './types.js';

// Web Awesome components
import '@awesome.me/webawesome/dist/components/card/card.js';

export class MyComponent extends LitElement {
  static get properties() {
    return {
      entityId: { type: Number, attribute: 'entity-id' },
      _data: { state: true },
      _loading: { state: true },
      _error: { state: true }
    };
  }

  declare entityId?: number;
  declare private _data?: MyData;
  declare private _loading: boolean;
  declare private _error?: string;

  static styles = css`
    :host { display: block; }
    /* Use wa- CSS variables for consistency */
    .container { color: var(--wa-color-primary-700); }
  `;

  willUpdate(changedProperties: Map<string | number | symbol, unknown>) {
    if (changedProperties.has('entityId') && this.entityId) {
      this._fetchData();
    }
  }

  private async _fetchData() {
    this._loading = true;
    this._error = undefined;
    try {
      const backendUrl = process.env.NEXT_PUBLIC_API_URL || '';
      const response = await fetch(`${backendUrl}/api/my-endpoint?id=${this.entityId}`);
      if (!response.ok) throw new Error('Failed to fetch');
      this._data = await response.json();
    } catch (e: unknown) {
      this._error = e instanceof Error ? e.message : 'An unknown error occurred';
    } finally {
      this._loading = false;
    }
  }

  render() {
    return html`
      <wa-card>
        <div slot="header">...</div>
        <main>...</main>
      </wa-card>
    `;
  }
}

if (!customElements.get('my-component')) {
  customElements.define('my-component', MyComponent);
}
```

### Key Differences from Legacy Pattern:
- Use `static get properties()` with `declare` fields instead of `@property()` / `@state()` decorators (avoids decorator compatibility issues).
- Use `willUpdate()` for reactive data fetching instead of `stateChanged()` (no Redux store).
- Register custom elements conditionally with `if (!customElements.get(...))` to avoid duplicate registration errors.

### Validation Error Handling (Declarative vs Imperative)
In the legacy system, `src/view/webComponent.ts` acted as an intermediary, aggressively traversing the DOM to call imperative functions like `setCustomValidity()`, `reportValidity()`, or manually toggle error CSS classes whenever a validation rule failed in a ViewModel.

In the modernized architecture, we rely entirely on **declarative state bindings** via Web Awesome properties:
1. The ViewModel (e.g., `LocationInputViewModel`) triggers a validation failure via `this.errorInfo.setError(...)`.
2. This triggers `notifyView(['errorInfo'])`, calling `modelUpdated()` on the Lit component.
3. The component extracts the message and sets a local reactive `@state() _errorText` property.
4. The component's `render()` function directly binds this string to the `<wa-input>` component's native properties:
   ```html
   <wa-input
       help-text=${this._errorText}
       ?invalid=${!!this._errorText}
   >
   ```
This natively paints the field red and shows the error text, completely eliminating the need for `webComponent.ts` or imperative DOM manipulation.

### Global Error Handling (Legacy \`Page.handleError\`)
In the legacy architecture, network exceptions or API failures were often caught and passed to a global singleton via \`Page.handleError(ex, { ajax: true })\`. This imperative approach took control away from the local component, globally traversing the DOM or redirecting the user based on the error.

In the modernized Next.js architecture, the global \`Page.handleError\` singleton is entirely removed. Instead, we use:
1. **Local Declarative State**: API failures are caught in the viewmodel/component and surfaced via standard \`_error\` state properties, triggering a declarative re-render to display the error message locally within the UI component.
2. **Next.js Error Boundaries**: For catastrophic or unrecoverable errors, we rely on standard \`console.error(ex)\` logs and allow Next.js React Error Boundaries (\`error.tsx\`) to natively catch the exception and degrade the UI gracefully, rather than relying on a custom global JS singleton.

### ViewModel Modernization (Data Binding & Lifecycle)
In the legacy architecture, the `ViewModel` base class (in `src/viewmodel.ts`) implemented a manual pub/sub event bus (`_propertyChange = new Event<PropertyChangeEventArgs>()`) for granular data binding, and an `initialize()` method to manually bootstrap views.

In the modernized architecture, Lit handles reactivity and component lifecycles natively:
1. **Reactivity**: We removed the `PropertyChange` event bus. The modernized `ViewModel` relies on `notifyView(props: string[])` which delegates to the view adapter's `modelUpdated(props)` method. Lit components then safely read the updated data into `@state()` or `@property()` fields and trigger their native asynchronous `requestUpdate()` rendering loop to efficiently update the virtual DOM.
2. **Lifecycle**: The base `initialize()` method was entirely stripped from the `ViewModel` because Lit handles initialization natively via standard web component callbacks like `connectedCallback()` and `firstUpdated()`.

## 5. React Integration

Since the project uses Next.js, Lit components must be wrapped in React for use in the App Router.

1. **Wrapper File**: Create `MyComponentWrapper.tsx` in the same directory.
2. **SSR**: Set `ssr: false` when using `next/dynamic` if the component relies on browser APIs (e.g., Lit).
3. **Typing**: Declare the custom element in `JSX.IntrinsicElements` with proper attribute types.

### Example Wrapper:
```tsx
'use client';
import dynamic from 'next/dynamic';

const MyComponentWrapper = dynamic(() =>
  import('./my-component.js').then(() => {
    return {
      default: ({ entityId }: { entityId: number }) => (
        <my-component entity-id={entityId} />
      ),
    };
  }),
  { ssr: false }
);

declare global {
  namespace JSX {
    interface IntrinsicElements {
      'my-component': React.DetailedHTMLProps<
        React.HTMLAttributes<HTMLElement> & { 'entity-id'?: number },
        HTMLElement
      >;
    }
  }
}

export default MyComponentWrapper;
```

### Server Components Data Fetching Pattern (Dynamic SEO & Legacy Tokens)
In the legacy app, complex routes relied on resolving JSON "navigation tokens" on the client, which harmed SEO and caused loading spinners. In this Next.js architecture, this logic must be moved to Server Components (`page.tsx`) to generate dynamic metadata and fetch data before passing it to the Lit client boundaries.

**Pattern Example:**
1. Fetch data in `generateMetadata` on the server.
2. Fetch data again in the `page.tsx` Server Component (Next.js automatically dedupes the request).
3. Pass the resolved JSON payload down to the Client Boundary (`'use client'`).

```tsx
// frontend/src/app/complex-route/page.tsx (Server Component)
import type { Metadata } from 'next';
import ComplexRouteClient from './ComplexRouteClient';

export async function generateMetadata({ searchParams }): Promise<Metadata> {
  const token = searchParams.token;
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/resolve?token=${token}`);
  const data = await res.json();
  
  return {
    title: data.seoTitle,
    description: data.seoDescription,
  };
}

export default async function ComplexRoutePage({ searchParams }) {
  const token = searchParams.token;
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/resolve?token=${token}`);
  const payload = await res.json();

  // Pass resolved payload to Client Boundary
  return <ComplexRouteClient initialData={payload} />;
}
```

```tsx
// frontend/src/app/complex-route/ComplexRouteClient.tsx (Client Boundary)
'use client';
import dynamic from 'next/dynamic';

const LegacyLitWrapper = dynamic(
  () => import('@/company/bundle').then((mod) => mod.LegacyLitWrapper),
  { ssr: false }
);

export default function ComplexRouteClient({ initialData }) {
  return <LegacyLitWrapper resolvedData={initialData} />;
}
```

## 6. CSS Guidelines

- **Vanilla CSS Only**: Use `css` tag from Lit in `static styles`.
- **Shadow DOM**: Respect Shadow DOM encapsulation; use `::part()` for styling internal component parts if exposed by Web Awesome.
- **Color Scheme**: Use `#4285f4` as the primary accent color (matching legacy `--page-accent-color`). Background `#f5f5f5`, text `#333`.
- **Material Design Shadows**: Use the standard 3-layer box-shadow: `0 2px 2px 0 rgba(0,0,0,0.14), 0 1px 5px 0 rgba(0,0,0,0.12), 0 3px 1px -2px rgba(0,0,0,0.2)`.
- **Font**: `Roboto, var(--wa-font-sans, sans-serif)`.
- **Web Awesome Base Path**: Set via `setBasePath()` for icon loading:
  ```typescript
  import { setBasePath } from '@awesome.me/webawesome/dist/utilities/base-path.js';
  setBasePath('https://cdn.jsdelivr.net/npm/@awesome.me/webawesome@3.8.0/dist/');
  ```

## 7. Legacy Source Reference

Legacy source files are located at `..\legacy\website\wwwroot\`. Key directories:
- `company/` — 16 page components (profile, articles, feed, home, job, jobs, marketplace, news, product, products, project, projects, promotions, search, header-layout, index)
- `component/` — Reusable building blocks (layout/card, search/box, search/category/menu, page/menu, map/view, etc.)
- `src/model/` — TypeScript models and interfaces
- `src/service/` — Service layer and API calls
- `src/navigation.js` — SPA routing and navigation tokens

## 8. Backend API & LINQ Guidelines

When porting legacy C# code that constructs complex LINQ queries (especially those using `FacetSet` filtering, SQL Table-Valued Functions (TVFs) like `CompanyOfficeLocation`, or hierarchical category unwinding):

- **Use Reusable Extension Methods**: Do not inline complex `Where` or `Join` clauses repeatedly in endpoint methods.
- **Implement `.GetFiltered()`**: Following the legacy codebase architecture, implement and use `public static IQueryable<T> GetFiltered(this IQueryable<T> query, AppDbContext dbContext, QueryInput queryInput)` extension methods (e.g., in `BizSrt.Api.Data.Extensions.QueryExtensions`).
- **Building Blocks**: Use these extension methods as composable "building blocks" that take the base `IQueryable<T>` (e.g., `dbContext.CompanyProfiles` or `dbContext.CompanyProducts`) and append the required SQL TVF joins for location searching or facet filtering before returning the modified query to the service method.
- **Stored Procedure Fallbacks**: When porting `Search` methods, ensure you maintain parity with the legacy dual-path architecture. If a user provides a `SearchQuery` or `SearchNear` parameter, bypass the complex LINQ queries entirely and execute the underlying database stored procedures (e.g., `CompanySearch`, `ProductSearch`) directly using `dbContext.Database.GetDbConnection().CreateCommand()`. The legacy complex LINQ structures should only be used when browsing by category without text search.

---
*Last updated: July 07, 2026*

## 9. Search Performance Patterns

These patterns were discovered and applied while porting the Company and Product search flows. Apply them to all future search ports (Project, Job, Community).

### 9.1 Frontend: Async/Promise Bridge in ViewModel `fetchList`

**Problem**: The legacy `Searchview.fetchList` base class dispatches the API call as a callback:
```ts
fetchDelegate(queryInput, callback, faultCallback); // legacy callback style
```
The legacy `service/company.ts` functions matched this signature: `search(queryInput, callback, faultCallback)`. In the modern port, all service functions are `async` and return a `Promise` — the extra callback arguments are silently dropped, so data never flows back into the ViewModel.

**Fix**: Override `fetchList` in the domain ViewModel and **bridge the Promise to the callback manually**. Do **not** call `super.fetchList(...)`. Build the queryInput from `this.searchParams` directly:
```ts
class CompanySearchViewModel extends Filterable(Searchview) {
  fetchList(queryInput: any, callback: Action<any>, faultCallback: Action<any>) {
    if (!this.searchParams) { faultCallback(new Error('No search params')); return; }
    queryInput.category = (this.searchParams as any).categoryId || 0;
    queryInput.location = (this.searchParams as any).locationId || 0;
    // ... other params ...
    search(queryInput).then(callback).catch(faultCallback); // Promise → callback bridge
  }
}
```
Also ensure the filter components return their **ViewModel** (not the element itself) from `getViewModel()`:
```ts
getViewModel(name: string) {
  if (name === 'filterAvail')   return (this.shadowRoot?.querySelector('list-filter-available') as any)?.viewModel;
  if (name === 'filterApplied') return (this.shadowRoot?.querySelector('list-filter-applied') as any)?.viewModel;
  if (name === 'listHeader')    return this.shadowRoot?.querySelector('list-header'); // element directly — no viewModel
}
```
Also call `Semantic.Facet.deserialize(data.facets)` inside the service `search()` function after parsing JSON to back-populate `FacetValue.name` references (required for the filter dropdown to display group names).

### 9.2 Backend: Avoid Triple-Query in LINQ Search

**Problem**: The LINQ search path runs the same expensive correlated subquery (`Categories_Unwound.Any()` + `CompanyProducts.Any()`) **three times**:
1. `CountAsync()` — for `TotalCount`
2. Facet aggregation `ToArrayAsync()` — for filter counts
3. Page result `ToArrayAsync()` — for the actual page

Each run re-evaluates the entire predicate including TVF joins, which is costly.

**Fix**: Materialize **all matching IDs** in a **single** query projecting only `Id` (SQL Server can satisfy this with a covering index, no column reads). Derive `total` from `.Length` in memory. Slice the page with LINQ `.Skip().Take()` in memory. Run facet aggregation against `allMatchingIds.Contains(c.Id)` — a simple `IN` clause:

```csharp
// Single DB round-trip for IDs (cheap — covering index, no data columns)
var allMatchingIds = await query
    .OrderByDescending(c => c.Created)
    .Select(c => c.Id)
    .ToArrayAsync();

var total = allMatchingIds.Length;
var pageIds = allMatchingIds.Skip(startIndex).Take(length).ToArray();
var companies = pageIds.Select(id => new SearchItem { Id = id }).ToArray();

// Facet aggregation on the pre-materialized set — no re-scan of the base predicate
var pfq = await (from c in dbContext.CompanyProfiles
                 where allMatchingIds.Contains(c.Id)
                 join pf in dbContext.CompanyFacets on c.Id equals pf.Company
                 ...
```

> [!NOTE]
> `allMatchingIds.Contains(c.Id)` generates `WHERE Id IN (...)`. SQL Server limits parameters to ~2100. For extremely large result sets, consider batching or a temp table approach. In practice the category filter narrows results well below this limit.

### 9.3 Backend: Avoid N+1 in Cache `ToPreview`

**Problem**: `CachedCompanyProfile.ToPreview()` accesses `Offices` and `Products` via lazy `GetArray()` getters, each hitting the DB individually per company. For 20 companies on a cold cache: ~40 individual queries → ~9s.

**Fix**: Batch-load all related collections in the cache's **multi-fetch constructor** alongside the profile query. Store them as plain `{ get; set; }` properties — no lazy loading:

```csharp
// In CompanyProfilesCache batch fetcher — 3 extra queries for all N companies, not N*3
var allOffices = dbContext.CompanyOffices
    .Where(co => accountIds.Contains(co.Company))
    .Select(co => new { co.Company, co.Id, ... })
    .AsNoTracking().ToList()
    .GroupBy(co => co.Company)
    .ToDictionary(g => g.Key, g => g.Select(...).ToArray());

var allProducts = dbContext.CompanyProducts
    .Where(cp => accountIds.Contains(cp.Company) && cp.UnlistedType == 0)
    .Select(cp => new { cp.Company, cp.Product })
    .AsNoTracking().ToList()
    .GroupBy(cp => cp.Company)
    .ToDictionary(g => g.Key, g => g.Select(cp => cp.Product).ToArray());

// Assign in the Select:
Offices = allOffices.GetValueOrDefault(p.Profile.Id, Array.Empty<CachedCompanyOffice>()),
Products = allProducts.GetValueOrDefault(p.Profile.Id, Array.Empty<long>()),
```

> [!CAUTION]
> Do **not** call `AsNoTracking()` on scalar projections like `.Select(cp => cp.Product)` — EF Core requires a reference-type entity for `AsNoTracking<TEntity>`. Only call it on entity or anonymous-type projections.

