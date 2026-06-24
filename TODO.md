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

## 3. SEO Metadata (Advanced)
- We have basic Next.js metadata, but we need to port the extensive **JSON-LD** schema, breadcrumbs, and canonical URLs that the legacy app generated for rich search engine indexing.

## 4. Remaining Legacy Pages
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

## 5. Mock Data / Database Validation
- Ensure all new components properly handle empty states if the live database (SQL Server instance) doesn't have data for a specific feature.
