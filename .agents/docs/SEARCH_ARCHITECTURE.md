# BizSort Search Architecture

This document outlines the end-to-end architecture for the search mechanism in BizSort, detailing how the frontend UI components (`search-home`, `search-category-input`, `search-location-input`) interact with Next.js routing and the backend caching layer.

## 1. Input Component Architectures

The category and location inputs share an identical UI pattern but diverge in strictness and hydration rules. Both components maintain two distinct, independent state variables:
1. `selected` (Object): The underlying resolved entity (e.g., `{ id: 123, name: 'Plumbers' }`).
2. `text` (String): The free-form text currently in the input box.

### Category Input (`search-category-input`)
- **Selection Behavior:** Typing into the input triggers an Autocomplete dropdown. Selecting an item sets `selected` and **explicitly clears** `text`.
- **Refined Searching (X in Y):** After selecting a category, clicking the input again allows the user to type a free-form string. The search engine resolves this as `query` *within* `category`.
- **Autocomplete Scoping (Global):** The category autocomplete is ALWAYS global. Typing "Bob" will query all categories for "Bob", regardless of whether "Plumbers" is currently selected.

### Location Input (`search-location-input`)
- **Dual Modes (`geoMode`):** Toggles between Database locations (`geoMode = off`) and Google Geocoding (`geoMode = on`).
- **Database Mode (`geoMode = off`):** Functions identically to Category (selecting a location clears text to allow refined searching). However, unlike Category, a selected location **CANNOT** be cleared with an "x" button since location is strictly required by the search engine.
- **Geocode Mode (`geoMode = on`):** Replaces the database location with a `searchNear` payload representing the user's geocoded coordinates.
- **Autocomplete Scoping (Country):** Location autocomplete is scoped to the globally configured `LocationSettings.country` (e.g., Canada). It searches all of Canada, rather than being restricted to the currently selected City/Province.

## 2. Search Aggregation & Navigation (`search-home` and `search-header`)

The `<search-home>` and `<search-header>` components act as the orchestrators. When the user clicks the Search button, they force validation on their children:
1. **What:** At least ONE of *Selected Category* or *Search Text* must be present.
2. **Where:** EXACTLY ONE of *Selected DB Location* or *Google Geocoded Point* must be present.

If valid, `search-home` yields a `Selection` payload to its parent (e.g., `<product-home>`), which dispatches it to `Navigation.go()`. The payload is converted into URL search parameters (e.g. `?categoryId=123&searchQuery=Bob&locationId=456`) and Next.js pushes the new route.

## 3. Hydration (`reflectToken`)

When a user navigates to a search results page or refreshes the browser, Next.js passes the URL parameters down into the viewmodels.

**The Hydration Problem:** The URL only contains the Category/Location **IDs**, not their human-readable names. We cannot fall back to assigning the search query string as the category name!

**The `reflectToken` Solution:**
When `loadSelection` pushes IDs into the input viewmodels, the viewmodels execute a `reflectToken` cycle to hydrate the inputs:
1. They fire an asynchronous API request using the specific `.get(id)` service method (`service/category.get(id)` / `service/location.get(id)`). This is strictly distinct from the `.autocomplete()` service method (e.g., `fetchCategories`) which is exclusively used for the dropdown search.
2. Once the API returns the requested entity, they overwrite `selected` with the true `{ id, name }`.
3. Concurrently, the free-form `text` string (e.g., `searchQuery=Bob`) is pushed independently into the viewmodel so the user sees both their refined search text and the background selected entity name.

## 4. Backend Autocomplete & SQL Caching

The frontend autocomplete behavior is completely agnostic to scoping logic—it blindly forwards its `_scope` payload to the backend C# endpoints, which orchestrate the heavy lifting via the `GroupSearchCache` and EF Core closure tables.

### The `parent` Parameter (The Filter)
When the frontend requests autocomplete, it passes its component scope as the `parent` ID:
- **Category:** Passes `0` (All Categories). The C# `CategorySearchCache` bypasses the `Categories_Unwound` hierarchy table and performs a global text search.
- **Location:** Passes `2` (e.g. Canada). The C# `LocationSearchCache` joins against `Locations_Unwound`, restricting the autocomplete results exclusively to children of Canada.

This is cached efficiently via a composite cache key: `new GroupSearchCache<int> { Parent = 2, Name = "Toronto" }`.

### The `scope` Parameter (The Formatter)
The frontend also passes the full `_scope` object. The C# endpoint ignores this for filtering and **only** uses it for formatting the display path via `location.AutocompletePath(scope.Id)`. This strips redundant root nodes (e.g., hiding ", Canada" from every result if the search is already scoped to Canada).

## 5. URL Parameter Escaping (Payload Serialization)

When passing search parameters to the backend via the `?queryInput={...}` JSON string (e.g. inside `service/company.ts` and `service/product.ts`), special care must be taken to manually escape certain user-provided free-form strings before JSON serialization:

- **`searchQuery`**: The primary free-form text input string.
- **`searchNear.text`**: The city/address string retrieved from Google Maps Geocoding (if `geoMode = on`).

**Why escape them?**
If a user inputs a special character (like `&` or `=`) into either of these strings, the resulting un-escaped JSON string (e.g., `{"searchQuery": "Bob & Sons"}`) will instantly truncate the HTTP query payload parser right at the `&`. This causes the C# backend to receive a truncated, malformed string (e.g., `{"searchQuery": "Bob `) resulting in a `JsonException`. 

To prevent this, we explicitly run `encodeURIComponent()` on **only** these two specific string fields before JSON serialization. 
On the hydration side (inside the Next.js page wrappers and viewmodels), we also apply a robust `decodeURIComponent()` fallback (e.g. checking `if (val.startsWith('%7B'))`) to safely parse any double-encoded `searchNear` payload objects that traverse back through the URL.

---
*Documented on July 8, 2026 for AI Agents and Developers maintaining the legacy parity architecture.*
