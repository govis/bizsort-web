# BizSort SPA Modernization Guide (Astro Architecture)

This document outlines how the legacy BizSort Single-Page Application (SPA) architecture has been modernized, focusing on the definitive pivot from our initial Next.js (React) experiment to the final **Astro + Lit** architecture.

## The Paradigm Shift: Why Astro over Next.js?

The legacy architecture relied heavily on a custom monolithic Client-Side Router (`web-main.ts` and `<page-view>`) built during the Polymer 2/3 era. 

Our initial modernization attempt used Next.js. However, Next.js forces a React Virtual DOM paradigm. Wrapping our self-contained Lit Web Components inside React (`bundle.tsx` and `next/dynamic` with `ssr: false`) proved to be unnecessary overhead that caused friction with layout encapsulation and event handling.

**The Astro Advantage**: Astro is fundamentally HTML-first and UI-agnostic. It allows us to keep the exact "Web Component App Shell" philosophy of the legacy application while gaining modern Vite bundling, Server-Side SEO, and file-system routing. We render our `<company-profile>` or `<company-home>` Lit elements natively in `.astro` files without any Virtual DOM wrappers.

## Concept Mapping: Legacy vs. Astro Modernization

The following table demonstrates how the custom concepts from the legacy architecture map to standard Astro equivalents:

| Legacy Concept | Modern Astro / Lit Architecture | Description of Change |
|---|---|---|
| `web-main.ts` App Shell | **Astro File-System Routing** | Instead of regex path matching in a massive JS array, we use Astro's file-system based routing (e.g., `src/pages/company/[id].astro`). |
| `<page-view>` Dynamic Loader | **Astro `<ViewTransitions />`** | The legacy router manually fetched JS chunks and swapped DOM nodes with CSS `@keyframes`. Astro's View Transitions API natively intercepts navigation, dynamically fetches the next page's HTML/JS, swaps the DOM seamlessly, and plays transition animations. |
| Lazy Loading (`import()`) | **Vite Code Splitting** | Placing a `<script>` tag in an Astro route tells Vite to automatically analyze dependencies and generate highly optimized, code-split JavaScript chunks for that specific page. |
| `PageModel` / `ViewModel` pattern | **Lit Element Lifecycle** | Separate ViewModel classes are flattened directly into the Lit component state and properties (`connectedCallback`, `willUpdate`), simplifying the mental model. |
| Redux `connect(store)` | **Direct Fetch / Local State** | Global state for window size is replaced by modern APIs like `ResizeObserver`. Data fetching is done via native `fetch` within Lit components, avoiding complex global stores. |
| Programmatic Nav (`Shell.go()`) | **Astro `navigate()` API** | Legacy helpers like `Company.search()` are preserved in `frontend/src/navigation.ts`, but now they simply pipe into Astro's `navigate(href)` to trigger a soft View Transition instead of manually manipulating `history.pushState`. |
| SEO Context Updates | **Astro Frontmatter (`---`)** | Instead of updating `document.title` and canonical links from a client-side ViewModel, we fetch data once in the Astro frontmatter and natively emit `<title>` and `<script type="application/ld+json">` tags. |
| URL Serialization | **Astro Edge Middleware** | Legacy `Token` JSON serialization in query strings (`?t={json}`) is intercepted by `src/middleware.ts` to issue instant 301 redirects to modern semantic URLs. |
| Auth Routing (`_validateToken`) | **Astro Edge Middleware & Cookies** | Client-side auth checks that abort navigation are replaced with Astro edge middleware redirecting to `/login` before rendering. |

## Deep Dive: Retiring the Legacy Routing Engine

The core legacy mechanism relied on three primary files located in `..\legacy\website\wwwroot\`:

### 1. `web-main.ts` (The Route Configurator)
* **Legacy Function**: Acted as the central routing table mapping regex URL paths to component names and physical file paths for lazy loading.
* **Astro Replacement**: **Completely Removed**. Astro's file-system routing natively solves this. By placing files in `src/pages/`, Astro inherently knows the route mapping.

### 2. `component/page/view.ts` (`PageView` / The Router Outlet)
* **Legacy Function**: This was the dynamic loading engine. It listened for route changes, used JavaScript's dynamic `import(elementPath)` to fetch the required JS bundle over the network, manually executed `document.createElement()`, injected the data, and handled CSS transition animations.
* **Astro Replacement**: **Completely Removed**. Astro's built-in `<ViewTransitions />` component handles this automatically at the browser level.

### 3. `src/navigation/routes.ts` & `navigation.js` (`Shell.go`)
* **Legacy Function**: Abstracted all programmatic navigation. Components would call `Shell.go()` or `Company.search()`, and this file would translate those commands into history events and handle the browser "Back" button (`popstate`).
* **Astro Adaptation**: **Kept and Adapted**. Your Lit components still rely heavily on these programmatic helpers to navigate without hardcoding URLs. We keep this file, but rip out the manual `history.pushState` logic, piping these helpers directly into Astro's `navigate(href)` API.

### 4. `src/navigation/token.ts` (State Serialization)
* **Legacy Function**: Serialized deeply nested application state into highly compact JSON objects for the URL, allowing the SPA to dynamically rehydrate its exact state when users refreshed or shared links.
* **Astro Adaptation**: **Kept and Adapted**. We must retain the ability to deserialize these legacy tokens to prevent millions of existing indexed URLs and bookmarks from breaking. We adapt this by moving the parsing logic into Astro's Edge Middleware (`src/middleware.ts`), redirecting legacy traffic transparently.

## The Lit + Astro Integration Strategy

To achieve a clean separation of concerns, we divide responsibilities cleanly:

1. **Lit Web Components (`frontend/src/**/*.ts`)**: Contain 100% of the interactive UI, styling, client-side fetching, and view model logic. They are treated as pure, framework-agnostic Custom Elements.
2. **Astro Pages (`frontend-astro/src/pages/**/*.astro`)**: Serve purely as the layout shell. They execute strictly on the server to:
   - Read URL parameters and fetch critical SEO data.
   - Inject OpenGraph tags, Title tags, and JSON-LD.
   - Emit the raw HTML Custom Element tags (e.g., `<company-home search-query="plumbers"></company-home>`).
   - Hydrate the page via lightweight `<script>` blocks.

By relying on Astro's native file-system routing and Vite's bundler, we get all the benefits of the legacy `PageView` dynamic loading with zero custom orchestration code and zero Virtual DOM wrappers to maintain.
