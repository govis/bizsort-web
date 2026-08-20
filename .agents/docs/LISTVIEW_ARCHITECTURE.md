# ListView Architecture & ViewModel Orchestration

This document describes the modern Astro/Lit architecture for rendering paginated, filterable lists of entities (e.g., `company-search`, `offering-search`, `company-offerings`). It traces the abstraction hierarchy of the legacy `PageModel(ListView)` ported to the modernized `View` and `Searchview` classes.

## 1. The ViewModel Class Hierarchy
The core architecture is built upon an inheritance chain of ViewModels that orchestrate child components.

1.  **`View` (Base Class):** Handles pagination state (`_pager`), coordinates the `fetchList()` API call to get a `QueryOutput`, manages `loading` / `empty` states, and orchestrates DOM components via `IViewAdapter`.
2.  **`Filterable(View)`:** A mixin applied to `View` that hooks into faceted search (e.g., `<list-filter-available>` and `<list-filter-applied>`). It intercepts `.search()` calls to include facet selections in the payload.
3.  **`Searchview extends View`:** Specialized for global search pages. It defines `searchParams` (like `searchQuery`, `categoryId`, `locationId`) and maps them into the `List.SearchInput` DTO.

## 2. Component Orchestration (The Triad)
A "Page" component (e.g., `CompanyOfferings`, `CompanySearch`) acts as the host. It instantiates a concrete ViewModel (e.g., `CompanyOfferingsViewModel`) and binds it to three critical child components via `this.viewModel.initialize()`:

1.  **`listView` (`<company-listview>`, `<offering-listview>`):** The visual grid/list of hydrated cards.
2.  **`listHeader` (`<list-header>`):** Displays the "Showing X of Y results" header, sort dropdowns, and view toggles (Grid vs. List).
3.  **`pager` (`<list-pager>`, `<list-page-select>`):** Controls pagination. The `View` engine natively constructs a `Pager` class and pushes it down to the UI components via `.master=${this.viewModel.pager}`.

**Initialization Gotchas:**
```typescript
firstUpdated() {
  // CRITICAL: Call initialize() without forcing 'listView: this'.
  // View.initialize() will automatically query the Shadow DOM for the 'list-header' and the respective listview,
  // extracting their inner viewModels and wiring up property delegation correctly.
  this.viewModel.initialize();
}
```

## 3. The Data Fetch Lifecycle (`fetchList` -> `toPreview`)
Because backend search indexes are heavily optimized, global queries (`SearchAsync`, `GetOfferingsAsync`) only return a lightweight `QueryOutput` containing raw `EntityId` arrays (or `SearchItem` references).

To display rich UI cards, these IDs must be hydrated into `Preview` models. The `View` orchestrates this two-step process:

1.  **`fetchList(queryInput, callback)`:** The ViewModel calls the API (e.g., `getOfferings`) and receives a `QueryOutput` containing `.series` (the array of IDs) and `.totalCount`. The `View` engine uses `.totalCount` to populate the `<list-header>` and configure the `Pager`.
2.  **`fetchPage(page, fetchAction)`:** The `View` engine slices the `.series` for the current page and invokes `fetchPage`. The ViewModel **must** implement this method by passing the sliced IDs to the `/profile/toPreview` API.
3.  **Hydration:** The `toPreview()` API hydrates the IDs and returns rich `OfferingPreview` / `CompanyPreview` objects. The `fetchAction` callback pushes these into the `listView.items` array, triggering the final DOM render.

**Tracing the Sequence:**
If a list is empty or cards are silently failing to render, trace the sequence:
`search()` -> `populate(0)` -> `fetchList()` -> `callback(QueryOutput)` -> `pager.populate(data)` -> `host.populatePage(page)` -> `fetchPage(page)` -> `toPreview()` -> `listItems = items` -> `listView.items = items`.

## 4. SSR vs. Client-Side Execution Contexts
Modern Astro allows these pages to be server-side rendered (SSR) for SEO, while retaining interactive pagination on the client.

**SSR Context (Astro Route):**
- The `.astro` file executes the two-step fetch (`getOfferings` -> `toPreview`) securely on the Node.js server.
- It serializes the final array of hydrated previews and passes them to the Lit component as an HTML attribute: `<company-offerings initial-items={initialItemsJson}>`.
- **Note:** In SSR, `API_BASE` resolves directly to the backend URL (`https://localhost:5001`).

**Client-Side Context (Lit Component):**
- **Hydration Bypass:** In `firstUpdated()`, the component checks if `this.initialItems` was provided by SSR.
- If provided, it can inject them directly into the ViewModel to avoid a redundant initial API fetch.
- **Dynamic Fetching:** When the user clicks "Next Page" or applies a facet filter, the ViewModel natively falls back to client-side fetching (`fetchList` -> `toPreview`).
- **Proxy Requirement:** Client-side requests route through the local Vite Proxy (`http://localhost:4321/api/...`) to bypass browser SSL strictness against the self-signed .NET certificate. `API_BASE` dynamically switches to `''` when executed in the browser.

## 5. Sliders vs. Full Lists
It is important to distinguish full paginated list pages from inline sliders (e.g., `offering-slider`, `company-slider`).
- Sliders **do not** use the complex `View` model orchestration. They simply extend `ListSlider<T>` and manage their own isolated `fetchPage()` state.
- Sliders map to legacy backend endpoints that return `SliceOutput` (which uses `.index` and `.length`), whereas full lists map to endpoints returning `QueryOutput` / `SearchOutput` (which uses `.totalCount` and `.series`).
