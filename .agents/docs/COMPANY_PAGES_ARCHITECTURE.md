# Company Pages Architecture

This document describes the modern architecture of the Company-related pages (Profile, Offerings, single Offering), detailing how Astro SSR (Server-Side Rendering) and Client-Side Lit components are seamlessly integrated via `@lit/context`.

## 1. Overview

The modernization of the Company profile pages involves a transition from the legacy client-heavy routing/fetching model to a modern hybrid SSR model. 

The three primary company views are:
- **Company Profile:** `/company/[companyId]`
- **Company Offerings List:** `/company/[companyId]/offerings`
- **Single Company Offering:** `/company/[companyId]/offering/[id]`

## 2. Astro SSR Integration

Astro serves as the foundational routing and SSR layer. Instead of sending an empty shell to the client and forcing the browser to fetch the company data, Astro fetches the data directly from the C# `.NET 10` API during the server request lifecycle.

### Server-Side Data Fetching
In the Astro page route (e.g., `src/pages/company/[companyId]/offering/[id].astro`), the server extracts the URL parameters and uses the shared TypeScript service helpers to hit the backend API:

```typescript
const { companyId: companyIdStr, id } = Astro.params;
const offeringId = parseInt(id as string, 10);
const companyId = parseInt(companyIdStr as string, 10);

// Fetches from the backend API during SSR
const offering = await viewOffering(offeringId, companyId);
const company = await viewCompany(companyId);
```

### JSON Serialization
The rich object data is serialized into a plain JSON string on the server:
```typescript
const offeringJson = JSON.stringify(offering || {});
const companyJson = JSON.stringify(company || {});
```

These JSON payloads are then injected directly into the initial HTML DOM as attributes on a specialized wrapper component.

## 3. The Role of `@lit/context` (Inversion of Control)

To prevent severe "prop-drilling" (passing the `company` object down through every intermediate layout and component), the architecture utilizes the `@lit/context` library to create a Provider pattern.

### `<company-header-layout>` as the Universal Provider
Every company-related page (Profile, Offerings List, Single Offering, etc.) naturally requires a header. Because the `<company-header-layout>` component is used as the universal wrapper for the main page content across all these views, it serves as the perfect Context Provider.

The page-level component simply passes the `company` object to the header layout:
```html
<company-header-layout .company="${this.company}">
    <!-- The entire page's content is slotted here -->
    <div class="company-profile-content">
        <list-header entity="offering"></list-header>
    </div>
</company-header-layout>
```

When the client-side JavaScript hydrates, `CompanyHeaderLayout` **provides** the object via a strongly-typed Context Token to its entire DOM subtree:

```typescript
export const companyContext = createContext<CompanyProfile | undefined>('company-context');

@provide({ context: companyContext })
declare company?: CompanyProfile;
```

### Context Consumers
Any deeply nested Lit component (like the Header, Tabs, or specific UI cards) can dynamically access the `companyProfile` without needing it passed as an HTML attribute by simply using the `@consume` decorator:

```typescript
@consume({ context: companyContext, subscribe: true })
@property({ attribute: false })
declare company: CompanyProfile;
```

## 4. Header Layout and Native Tabs

The `<company-header-layout>` (in `src/company/header-layout.ts`) is the primary shell for all company pages. It consumes the `companyContext` to render the company banner, logo, and tabs.

### Native Tabs Migration
Previously, the tabs were injected via a complex `<div slot="tabs">` mechanism from each individual page. This has been modernized into **Native Tabs**. 

The `company-header-layout` now intrinsically owns and renders the `<wa-tab-group>` and `<wa-tab>` elements. It accepts an `active-tab` attribute from the parent Astro page to highlight the correct tab on initial load.

### Multi-Product Routing Parity
The legacy backend dictates UI rendering via enums (e.g., `OfferingsView.MultiOffering`). The modern header perfectly mirrors this legacy logic:
- If a company is configured for `MultiOffering`, clicking the "Offerings" tab does **not** navigate to the `/offerings` page.
- Instead, it dispatches a `@tab-change` event that the `company-profile` catches to render the `multiOffering` rich-text HTML block directly inline.
- For all standard configurations, tab clicks delegate to the centralized routing facade `Company.tabView(companyId, tab)` in `navigation.ts`.
