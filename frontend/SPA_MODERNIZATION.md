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
| Programmatic Nav (`Shell.go()`) | `window.location.href` / `next/navigation` | Client-side transitions are handled by Next.js `<Link>` tags or `useRouter()`. |
| SEO Context Updates | **Next.js `generateMetadata`** | Instead of updating `document.title` and canonical links from a client-side ViewModel, Next.js generates static SEO metadata dynamically via server components. |

## Hybrid Component Strategy (React + Lit)

To achieve the best of both worlds, we employ a **Wrapper Pattern** using `*Wrapper.tsx` files:

1. **Lit Web Component (`src/company/profile.ts`)**: Contains the styling, template, and client-side logic for the specific bundle page.
2. **Bundle Layout (`src/company/header-layout.ts`)**: The Lit web component defining the shared shell (tabs, logo) for all pages within the bundle.
3. **React Wrappers (`src/company/*Wrapper.tsx`)**: These are crucial **bridge components** that seamlessly integrate the legacy Lit custom elements into the Next.js React ecosystem.
   - **Client-Side Boundaries**: Lit components manipulate the DOM and require browser APIs. These wrappers explicitly declare the `"use client"` directive to tell Next.js to render them entirely on the client-side.
   - **React to Lit Translation**: We utilize the `@lit/react` library (specifically the `createComponent` function) inside these wrappers. This creates a strongly-typed React component wrapper around the vanilla Lit web component.
   - **Props & Events**: The wrapper allows us to safely pass React props down to the Lit elements and bubble custom DOM events back up to the Next.js application if necessary.
4. **Next.js Page/Layout (`src/app/company/[id]/page.tsx`, `layout.tsx`)**: The server-rendered route groups that extract parameters from the URL, generate SEO metadata, and import/serve the React Wrappers.

### Routing Example: Company Page Bundle
* **Legacy:** URL `/company-profile/123` matched regex `^/company-profile/`. `Routes` engine dispatched an event to `<page-view>`, which fetched `company.bundle.js`. The bundle executed, instantiating `<company-profile>`, which internally wrapped itself in `<company-header-layout>`.
* **Modern:** URL `/company/123` hits `app/company/[id]/page.tsx`. Next.js extracts `params.id`. It automatically applies the bundle's shared shell defined in `app/company/[id]/layout.tsx`. The layout renders the `CompanyHeaderWrapper`, which wraps the `ProfileWrapper` page content. Code-splitting happens automatically per Next.js route segment.

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
