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

---
*Last updated: June 18, 2026*
