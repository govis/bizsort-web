# BizSort Web - TODOs & Future Tasks

This document tracks the remaining tasks for porting the legacy BizSort application to the modern Next.js + Lit + Web Awesome stack.

## 1. High Priority: Featured Sections (Profile Page)
The core profile page is ported, but the following featured sections need to be implemented using existing backend endpoints:
- **Product Slider**: Port the featured products carousel.
- **Affiliations Slider**: Port the company affiliations carousel.
- **Communities Slider**: Port the company communities carousel.

## 2. Header & Search Enhancements
- ~~**Condensing Header**: Implement the legacy scroll-condensing behavior in `company-header-layout` (where the logo dynamically resizes and hides when the user scrolls down).~~ ✅ (Completed)
- ~~**Location-Aware Category Search**: Update the `search-category-menu` stub to support "in [city]" and "near [postal code]" utilizing geolocation data, matching the legacy functionality.~~ ✅ (Completed)
- **Refactor `reflectToken` (Global Data Flow)**: We need to modify/refactor the `reflectToken` logic across the board (including `CategoryInputViewModel` and `LocationInputViewModel`). Instead of pulling from a global token, components must be refactored to read these values dynamically from Next.js `params` / `searchParams` passed down as React props from the Server Components.
- ~~**Implement `Validateable` Rules for Location Input**~~ ✅ (Completed): The `LocationInputViewModel` has ported its `Validateable` rules, including calling `_geoinput.validate()` and `_geoinput.resolve()` to translate Google Places into database IDs.

## 3. Global Components (App Shell)
- **Implement `message-toast`**: Port the legacy global toast notification system into a client component wrapper rendered in the Root Layout.
- **Implement `signin-form`**: Port the universal sign-in modal/form into a client component wrapper rendered in the Root Layout.

## 4. SEO Metadata (Advanced)
- We have basic Next.js metadata, but we need to port the extensive **JSON-LD** schema, breadcrumbs, and canonical URLs that the legacy app generated for rich search engine indexing.

## 5. Remaining Legacy Pages
Currently, only the main `profile` and `home` pages are ported. The Next.js App Router (Nested Layouts) architecture is now fully established, replacing the legacy `web-main.ts` routes. The `frontend/src/company/` directory needs components for the following legacy company pages:
- `articles`
- `feed`
- ~~`home` & SPA Architecture Modernization~~ ✅ (Completed)
- `index`
- `job` & `jobs`
- `marketplace`
- `news`
- `product` & `products`
- `project` & `projects`
- `promotions`
- `search`

## 6. Mock Data / Database Validation
- Ensure all new components properly handle empty states if the live database (SQL Server instance) doesn't have data for a specific feature.
