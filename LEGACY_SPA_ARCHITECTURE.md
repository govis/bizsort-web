# Legacy SPA Architecture Deep Dive

This document provides a deep dive into the legacy SPA (Single Page Application) switching mechanism, its components, their roles, and how they translate to the modernized Next.js App Router architecture. It serves as a periodic reference while refining the modern implementation.

## 1. Legacy Architecture Components

The legacy system was a heavily orchestrated Client-Side SPA built during the Polymer 2/3 era. The core mechanism relied on three primary files located in `..\legacy\website\wwwroot\`:

### A. `web-main.ts` (The Shell & Router Config)
- **Role**: Acted as the main application shell and router configuration.
- **Specifics**: 
  - Defined a central static `_routes` array mapping regex URL paths to Lit element names and file paths (e.g., `{ path: '^/company-profile', elementName: 'company-profile', elementPath: 'company/profile.js' }`).
  - Managed global user sessions (`User.statusChanged`).
  - Rendered a custom `<page-view>` tag (the viewport for the router) and passed it the global `navigationContext`.

### B. `src/navigation/routes.ts` & `navigation.js` (Routing Config & History)
- **Role**: Handled URL generation, `popstate` interception, and matched URLs to components.
- **Specifics**:
  - **`routes.ts`**: Exposed the `IRoute` interface and `Routes` class. It used regex matching to map a `path` (like `^/company-profile`) to an `elementName` (e.g., `company-profile`) and a `bundleName` for lazy loading. It also listened to browser `popstate` events to natively handle "Back" and "Forward" navigation.
  - **`Shell.go` (`navigation.js`)**: Intercepted programmatic navigation events, serialized the `Token` into a URL query string or location hash, and triggered `history.pushState()`.

### C. `src/navigation/token.ts` (The `Token` Definition)
- **Role**: Defined the complex `Token` object used to serialize deeply nested application state into the URL.
- **Specifics**:
  - Navigation state wasn't just a URL path; it was serialized into JSON `Token` objects containing `Action` enums (e.g., `View`, `Edit`), entity IDs, and even nested `Forward` or `Cancel` tokens.
  - Maintained `NavigationProperty` enums to map domain concepts (`ACCOUNT_ID`, `SEARCH_QUERY`, `SEARCH_NEAR`) to numeric keys in a `properties` dictionary to save bytes when serialized.
  - Implemented `Serialize()` and `Deserialize()` methods to pack/unpack this complex state into a compact format suitable for the URL query string or HTML5 History API state object.

### D. `component/page/view.ts` (`PageView` Component)
- **Role**: The heart of the routing engine (the Viewport). It managed the DOM and transitions.
- **Specifics**:
  - Listened for changes to the `navigationContext`.
  - When the URL changed, it determined the target element (`company-profile`).
  - Dynamically imported the necessary JS bundle via `import()`.
  - Manually created the new DOM node via `document.createElement(navigationContext.element)`.
  - **Injected the Data**: It directly attached the `Token` to the new element's `.model` and called `model.load()` (e.g., `incomingPage.model = newModel`).
  - **Animations**: Applied CSS `@keyframes` (like `slide-left` or `fade-in`) to orchestrate the entry of the new DOM node and the retirement of the outgoing DOM node.

---

## 2. Modernization Strategy

The detailed strategy for translating these legacy concepts into the modern Next.js App Router (including Next.js Middleware, React Props, Server Components, and the layout conventions) has been consolidated into the primary SPA modernization guide. 

👉 **Please refer to [SPA_MODERNIZATION.md](file:///C:/Bizsort/bizsort-web/frontend/SPA_MODERNIZATION.md) for the complete Modernization Strategy and Deep Dive.**
