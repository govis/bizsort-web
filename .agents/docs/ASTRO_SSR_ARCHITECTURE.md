# Astro SSR & Client Hydration Architecture

This document details the exact setup, pitfalls, and strict rules required when building Astro SSR pages that wrap Lit Web Components, specifically for data-heavy pages like company-profile and company-offering.

**CRITICAL:** Failure to follow these rules will result in catastrophic "Blank Page" errors, 404 Not Found errors for API and Image requests, or silent Lit hydration failures.

## 1. The SSR Data Flow (Astro to Lit)

When rendering a page server-side in Astro (e.g. src/pages/company/[companyId]/offering/[id].astro), the data is fetched natively in Node.js and injected into the HTML.

### Step 1: Astro Server Fetching
Astro runs the API fetch during the request lifecycle.
`	ypescript
// Astro Frontmatter (Server-Side)
const offering = await viewOffering(offeringId, companyId);
`

### Step 2: JSON Serialization
Astro cannot pass live JavaScript object references to the client. The rich API payload must be explicitly serialized into a JSON string before injecting it into the Lit component tag.
`	ypescript
// Always fallback to {} to prevent JSON.stringify(undefined)
const offeringJson = JSON.stringify(offering || {});
`

### Step 3: HTML Injection
The JSON string is injected into the HTML attribute.
`html
<company-offering offering={offeringJson}></company-offering>
`
*Note: Because Astro doesn't know about Lit, this renders purely as an unhydrated standard HTML string (e.g. <company-offering offering="{&quot;id&quot;:123...}">).*

## 2. Lit Client Hydration Rules

For Lit to successfully "wake up" and parse the JSON string back into a reactive object, the component **must** be strictly configured to intercept the attribute.

### The 	ype: Object Requirement
In the Lit component (e.g. src/company/offering.ts), the property must be explicitly declared with 	ype: Object in the static properties block. 

`	ypescript
export class CompanyOffering extends LitElement {
  static get properties() {
    return {
      offering: { type: Object }
    };
  }

  // CRITICAL: You must use declare to prevent the compiler from overriding Lit's reactive getter/setter
  declare offering?: any;
}
`

**Why this matters:** When the browser parses the DOM, Lit intercepts the offering attribute. Because it sees 	ype: Object, Lit automatically unescapes the HTML entities and calls JSON.parse() on the string, seamlessly restoring it to a rich object accessible via 	his.offering. If 	ype: Object is missing, 	his.offering will be treated as a literal string, breaking all object property access (e.g. 	his.offering.title will be undefined).

### Removing Legacy Client Fetching
When modernizing a component to use SSR, **you must completely remove all legacy client-side fetching logic (e.g., _fetchData(), companyId / offeringId state triggers, and loading spinners).**
- If you leave legacy willUpdate() triggers that look for companyId, but Astro is passing the full company={companyJson} payload, the component will never trigger its fetch and will render "Data not found".
- The component must assume the data is perfectly delivered via the property.

## 3. The API_BASE Trap (CORS and SSR)

The API configuration in src/service/api.ts dictates how network requests are routed. The transition from Vite Dev Server to Astro Node Adapter introduces a critical split in network topography.

`	ypescript
// src/service/api.ts
export const API_BASE = import.meta.env.SSR ? 'https://localhost:5001' : '';
`

### Client-Side (Browser)
- When import.meta.env.SSR is false, API_BASE is ''.
- The browser fetches /api/company/profile/view.
- **In Development (
pm run dev):** Vite's internal proxy (on 4321) catches /api and forwards it to the C# Backend (https://localhost:5001). This bypasses CORS and works perfectly.
- **In Production (
ode dist/server/entry.mjs):** The Astro Node server (on 4322) **does not have a proxy**. Client-side /api requests will 404 Not Found.

### Server-Side (Astro Node)
- When import.meta.env.SSR is true, Astro is executing the fetch natively within Node.js.
- Astro cannot fetch /api/... from itself because it doesn't have the backend data.
- **CRITICAL:** API_BASE **must** be hardcoded to the C# Backend URL (https://localhost:5001) during SSR. 
- If API_BASE is wrong (e.g., pointing to an offline port like 5000), the Astro server will fail to fetch the data, fallback to {}, and the client will render a Blank Page because the payload was empty!

### Rule of Thumb for Debugging Blank Pages
If a page renders "Data not found" or an image returns 404:
1. **Check the Network Tab:** Did the HTML arrive with company="{...}" fully populated? If it's company="{}", the Astro SSR fetch failed (Check API_BASE target and Backend health).
2. **Check the Lit Properties:** Is 	ype: Object correctly defined? If not, Lit failed to parse the JSON.
3. **Check the Port:** Are you viewing the Production Node build (4322) without a proxy? If so, client-side images and API calls will 404. Use the Vite Dev Server (4321) for local testing.
