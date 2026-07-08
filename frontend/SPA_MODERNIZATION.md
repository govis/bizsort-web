# BizSort SPA Modernization Guide

This document outlines how the legacy BizSort Single-Page Application (SPA) architecture has been modernized. We transitioned from a custom-built, purely client-side router and state manager to a modern **Next.js App Router** paired with **Lit** for Web Components.

## Architecture Paradigm Shift

The legacy architecture relied heavily on a monolithic `web-main.ts` file that acted as the application shell. It defined an extensive array of route regex patterns and relied on a custom `<page-view>` element to dynamically fetch JavaScript bundles over the network and inject elements into the DOM. State was managed via a custom, Redux-like `connect(store)` implementation.

The new architecture embraces a hybrid approach:
- **Next.js (React)** handles routing, server-side data fetching (where applicable), SEO metadata, and bundle splitting.
- **Lit** handles the complex, interactive client-side components and presentation logic.

## Concept Mapping: Legacy vs. Modern

The following table demonstrates how the custom concepts from the legacy architecture map to standard modern equivalents:

| Legacy Concept | Modern Next.js / Lit Architecture | Description of Change |
|---|---|---|
| `web-main.ts` App Shell | `app/layout.tsx` & App Router | The Next.js root layout replaces the custom shell. Next.js handles the overall page structure and standard headers/footers. |
| Page Bundles & Bundle Pages | **Next.js Nested Layouts (`layout.tsx`)** | Legacy bundles grouped domain-specific pages under a shared header (e.g., `company-header-layout`). We use Next.js nested layouts (`app/company/[id]/layout.tsx`) to provide these bundle-specific shells around the bundle's pages. |
| Custom `Routes` Engine | **Next.js App Router (`app/`)** | Instead of regex path matching in a JS array, we use Next.js's file-system based routing (e.g., `app/company/[id]/page.tsx`). |
| Lazy Bundle Loading (`bundle.js`) | **Next.js `dynamic()` imports** | Instead of defining bundles in the routing array, Next.js automatically code-splits per route. We use `dynamic(() => import('...'), { ssr: false })` to lazily load Lit Web Components. |
| `<page-view>` Container | **React Wrappers** | Instead of a custom element swapping out DOM nodes, we use React component wrappers (e.g., `<HomeWrapper />`) that render the underlying custom elements. |
| `PageModel` / `ViewModel` pattern | **Lit Element Lifecycle** | The separate ViewModel classes are flattened directly into the Lit component state and properties (`connectedCallback`, `willUpdate`), simplifying the mental model. |
| Redux `connect(store)` | **Direct Fetch / React Context** | Global state for window size is replaced by modern APIs like `ResizeObserver`. Data fetching is done via native `fetch` within Lit components, avoiding complex global stores. |
| Programmatic Nav (`Shell.go()`) | **`frontend/src/navigation.ts` Helpers** | Domain-specific namespaces (e.g., `Company.profileView()`) construct parameter bags and pipe into a global `Navigation.go()` event dispatcher, caught by a React wrapper (`NavigationProvider.tsx`) that triggers `useRouter()` for SPA transitions. |
| SEO Context Updates | **Next.js `generateMetadata`** | Instead of updating `document.title` and canonical links from a client-side ViewModel, Next.js generates static SEO metadata dynamically via server components. |
| URL Serialization | **Next.js Middleware** | Legacy `Token` JSON serialization in query strings is caught by Middleware and 301 redirected to modern semantic URLs. |
| Global Shell UI | **Next.js `loading.tsx` / `layout.tsx`** | Legacy `<img src="bizsort-logo.svg">` placeholders map to `<Suspense>` boundaries via `loading.tsx`. Global overlays (like `message-toast` or `signin-form`) are mounted directly in the `RootLayout` (`layout.tsx`). |
| Auth Routing (`_validateToken`) | **Next.js Middleware & `cookies()`** | Client-side auth checks that abort navigation are replaced with edge middleware redirecting to `/login` before rendering. |
| Initial State (`reflectToken`) | **Server-to-Client React Props** | Components dynamically reacting to global URL state now receive `params` directly as strictly typed React props from the server, bound to the component via Lit's `willUpdate`. |

## Deep Dive: Revamping the Legacy Routing Engine

The legacy system was a heavily orchestrated Client-Side SPA built during the Polymer 2/3 era. The core mechanism relied on three primary files located in `..\legacy\website\wwwroot\`:

### 1. `src/navigation/routes.ts` & `web-main.ts` (Routing Config & Shell)
- **Legacy**: `web-main.ts` acted as the main application shell and router configuration, exposing the `Routes` class to map regex paths (like `^/company-profile`) to Web Components (e.g., `company-profile`) and bundle names for lazy loading. It also listened to browser `popstate` events to natively handle "Back" and "Forward" navigation.
- **Modern Next.js**: The entire custom regex engine is replaced by Next.js **File-System Based Routing**. Folders in `frontend/src/app/` automatically define the route structure. For example, `{ path: '^/company-profile', elementName: 'company-profile' }` directly translates to `frontend/src/app/company/[id]/page.tsx`.

### 2. `src/navigation/token.ts` & `navigation.js` (The `Token` Definition)
- **Legacy**: Navigation state wasn't just a URL path; it was serialized into complex JSON `Token` objects containing `Action` enums (e.g., `View`, `Edit`), entity IDs, and even nested `Forward` or `Cancel` tokens. `navigation.js` intercepted programmatic navigation, serialized the `Token` into a URL query string (`?t=...`), and triggered `history.pushState()`.
- **Modern Next.js**: 
  - **Simple Navigation**: Handled by domain-specific semantic helper methods in `frontend/src/navigation.ts` (e.g., `Company.home()`, `Product.search()`, `Company.profileView()`). These methods construct transient parameter bags and route to standard semantic URLs (e.g., `/company/123`), piping the action through a global `Navigation.go()` dispatcher to maintain soft navigation via Next.js's `useRouter()`. This explicitly replaces manual `window.location.href` assignments across Lit components.
  - **Translating Legacy Tokens**: To maintain backwards compatibility (e.g., bookmarks pointing to `?t={json}`), we use **Next.js Middleware (`middleware.ts`)** to intercept requests on the Edge. It parses the legacy JSON string, maps the legacy IDs, and instantly issues an HTTP 301 redirect to the modern folder-based route, cleanly funneling legacy traffic without polluting the application logic.

### 3. `component/page/view.ts` (`PageView` Component)
- **Legacy**: The heart of the routing engine. It listened for URL changes, determined the target element, dynamically imported the necessary JS bundle via `import()`, manually created the new DOM node, and orchestrated CSS `@keyframes` animations to swap views. It dynamically attached the `Token` to the new element's model (data injection).
- **Modern Next.js**: Next.js automatically handles chunking, lazy loading, and DOM swapping out of the box. Data flows strictly top-down: the Server Component (`page.tsx`) fetches the required data and renders a Client Boundary wrapper, passing the data as React props. The wrapper then renders the Lit component, relying on standard Lit reactivity (`willUpdate`).

## Hybrid Component Strategy (React + Lit)

To achieve the best of both worlds, we employ a unified **Client Bundle Boundary** using a single `bundle.tsx` per domain (e.g., `src/company/bundle.tsx`):

1. **Lit Web Components (`src/company/*.ts`)**: Contains the styling, templates, and client-side logic for the specific bundle pages (e.g., `home.ts`, `profile.ts`, `header-layout.ts`).
2. **Unified React Client Boundary (`src/company/bundle.tsx`)**: This file serves as the singular entry point to bridge the Server-Side Next.js app with the Client-Side Lit components.
   - **Client-Side execution (`"use client"`)**: Lit components manipulate the DOM and require browser APIs (`window`). This bundle explicitly declares `"use client"` so it executes entirely in the browser.
   - **Centralized Custom Element Registration**: Like the legacy `directory.js` bundles, this file centrally imports all `.ts` Lit components for the domain, ensuring they are registered in the browser's `customElements` registry.
   - **React 19 Native Custom Elements**: Because Next.js 15 uses **React 19**, we no longer need translation libraries like `@lit/react` (`createComponent`). React 19 natively maps props and DOM events to custom elements! We simply export lightweight wrapper functions that return native JSX like `<company-profile company-id={id}></company-profile>`.
3. **Next.js Server Pages (`src/app/company/...`)**: The server-rendered route groups dynamically import the wrappers from `bundle.tsx` via `next/dynamic`.

### Routing Example: Company Page Bundle
* **Legacy:** URL `/company-profile/123` matched regex `^/company-profile/`. `Routes` engine dispatched an event to `<page-view>`, which fetched `company.bundle.js` (or `directory.js`). The bundle executed, defining all custom elements and instantiating `<company-profile>`.
* **Modern:** URL `/company/123` hits `app/company/[id]/page.tsx`. Next.js dynamically imports the `bundle.tsx` for the company domain. The bundle automatically registers the Lit custom elements and returns the `<company-profile>` element inside the shared `<company-header-layout>`. Code-splitting happens automatically per domain bundle.

## Server-Side SEO & Structured Data (JSON-LD)

In the legacy architecture, SEO optimizations (like `document.title`, `<meta>` tags, and `Schema_org` structured data) were built via client-side ViewModels. This required crawlers to execute JavaScript to index the pages.

In the modernized Next.js architecture, **all SEO generation executes on the Server Side**:

1. **Dynamic Metadata (`<title>`, `<meta>`, OpenGraph)**:
   Instead of static objects, dynamic routes (e.g., `app/company/[id]/page.tsx`) export a `generateMetadata` async function. Next.js fetches the required data server-side and automatically injects the tags into the `<head>` of the initial HTML response.
2. **JSON-LD (`Schema_org`)**:
   Structured data payloads (e.g., `LocalBusiness` schemas) are generated directly within the Server Component's render path and natively injected into the DOM as `<script type="application/ld+json">`.

Because these execute entirely on the Node.js server, web crawlers receive fully populated, SEO-optimized HTML immediately on their first network request.

## Styling and Components

- **CSS:** Standardized on Lit's scoped `css` template literal for encapsulated styling. 
- **Component Library:** Migrated from legacy Polymer/Material components to modern WebAwesome (`@awesome.me/webawesome`).

## Next Steps for Migration
As additional legacy pages are ported (e.g., products, jobs, community), follow this pattern:
1. Create the server-rendered Next.js `page.tsx` route.
2. Build the Lit component in `src/`.
3. Connect them via a client-side React wrapper.
4. Replace legacy `connect(store)` logic with direct REST API calls using `fetch`.
