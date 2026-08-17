# BizSort Web - Astro Modernization TODOs

This document tracks the tasks required to fully transition the frontend from Next.js (React) to **Astro**, taking full advantage of Astro's native HTML/Web Component support to drop React wrappers entirely.

## 1. Astro Migration Core (In Progress)
- [x] **Initialize Astro**: Set up `frontend-astro` with Vite config, `process.env` shims, and Lit support.
- [x] **Un-wrap Profile Page**: Restored `<company-header-layout>` inside `frontend/src/company/profile.ts` so the Web Component manages its own layout shell again.
- [x] **Astro Profile Route**: Created `src/pages/company/[id].astro` to fetch data server-side and render `<company-profile>` natively without React.
- [ ] **Migrate Middleware**: Port the Next.js `middleware.ts` logic (legacy `?t=` token redirection) to Astro's `src/middleware.ts`.
- [ ] **Un-wrap Remaining Pages**: Remove React wrappers (`bundle.tsx`) and reintegrate layout logic into:
  - `company/home.ts`
  - `company/search.ts`
  - `product/home.ts`
  - `product/search.ts`
- [ ] **Astro Routes**: Create corresponding `.astro` routes for the above pages in `src/pages/`.
- [ ] **SPA Routing (ViewTransitions)**: Add `<ViewTransitions />` to Astro layouts and update `Navigation.go()` in `frontend/src/navigation.ts` to use Astro's `navigate()` API for soft page transitions.
- [ ] **Cleanup**: Delete Next.js specific files (`frontend/src/app/`, `bundle.tsx`, `next.config.ts`, etc.) and move `frontend/src` directly into `frontend-astro`.

## 2. Featured Sections (Profile Page)
The core profile page is ported, but the following featured sections need to be implemented using existing backend endpoints:
- [ ] **Product Slider**: Port the featured products carousel.
- [ ] **Affiliations Slider**: Port the company affiliations carousel.
- [ ] **Communities Slider**: Port the company communities carousel.

## 3. Header & Search Enhancements
- [x] **Condensing Header**: Implemented legacy scroll-condensing behavior.
- [x] **Location-Aware Category Search**: `search-category-menu` stub updated.
- [ ] **Refactor `reflectToken` (Global Data Flow)**: Components must dynamically read URL parameters instead of relying on global tokens.
- [x] **Implement `Validateable` Rules for Location Input**: `LocationInputViewModel` ported.

## 4. Global Components (App Shell)
- [ ] **Implement `message-toast`**: Port the legacy global toast notification system into a client component wrapper rendered in the Astro Layout.
- [ ] **Implement `signin-form`**: Port the universal sign-in modal/form.

## 5. Remaining Legacy Pages
Once the Astro core is established, we need to port the remaining legacy pages to Lit/Astro:
- `articles`, `feed`, `job`, `jobs`, `marketplace`, `news`, `product`, `products`, `project`, `projects`, `promotions`, `search`.

## 6. SEO Metadata & JSON-LD
- [x] Basic dynamic SEO injection via `.astro` frontmatter.
- [ ] Full JSON-LD schema generation for companies and products server-side.
