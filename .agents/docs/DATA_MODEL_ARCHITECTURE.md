# Modernization Architecture: Rich Models vs Plain Interfaces

## The Problem
During the legacy application's lifespan, the frontend heavily relied on rich Object-Oriented Programming (OOP) classes. Models like `ResolvedLocation`, `Address`, and `Image` were not just data shapes; they contained embedded logic, getters (e.g., `get city()`, `get imageRef()`), and methods (e.g., `equalsTo()`).

When data was fetched from the legacy backend, a centralized engine (`Node.deserialize()`) recursively parsed the raw JSON and instantiated these classes.

In the transition to **Astro and Lit**, data fetching was simplified to standard `fetch().then(res => res.json())` calls, which return plain, uninstantiated JavaScript objects. As a result, the rich model classes ported to `frontend-astro/src/model/` are never actually instantiated. **All prototype methods, getters, and embedded logic have been completely stripped of functionality** and are currently dead code.

---

## Architectural Constraints in Modern Frameworks

Before deciding on a path forward, we must consider how modern frameworks behave:

1. **The SSR Network Boundary:** Astro relies on Server-Side Rendering (SSR). Data fetched on the server is serialized to a string (`JSON.stringify`) to be sent to the client. Functions and class prototypes cannot cross this boundary.
2. **Reactivity systems:** Modern tools like Lit and React rely on immutability (shallow reference checks) to trigger UI updates. Deeply mutating a rich class does not trigger re-renders natively.
3. **Tree-Shaking:** Bundlers (Vite/esbuild) cannot statically analyze which methods of a class are actually used. Including rich classes results in larger Javascript payloads being shipped to the browser, whereas plain TypeScript `interfaces` compile down to zero bytes.

---

## Proposed Solutions

### Option 1: The Modern Functional Approach (Recommended)
Refactor the architecture to completely abandon rich OOP models on the frontend. Convert all models to plain TypeScript `interfaces` (anemic models) and extract all embedded logic into pure, stateless utility functions grouped by domain.

**Example:**
```typescript
// Legacy OOP:
const city = myLocation.city;
const isMatch = myAddress.equalsTo(cachedAddress);

// Modern Functional:
import { getCity, isAddressEqual } from '../service/location';
const city = getCity(myLocation);
const isMatch = isAddressEqual(myAddress, cachedAddress);
```

**Considerations:**
- **Pros:** Perfect compatibility with Astro's SSR boundaries. Excellent performance (no deserialization overhead). Zero-byte bundle footprint for models (erased by TS). Highly testable pure functions.
- **Cons:** Requires manually hunting down every broken getter and method invocation across the ported components and rewriting them to use imported utilities.

### Option 2: Full OOP Rehydration (The Legacy Path)
Resurrect the legacy `Node.deserialize()` engine (or write a modern equivalent). Intercept all API responses in the `service` layer and recursively instantiate the plain JSON into their respective rich classes before returning them to the UI components.

**Considerations:**
- **Pros:** Minimizes code changes inside the actual UI components. The existing ported logic (`this.location.city`) will instantly start working again.
- **Cons:** Blocks the main thread during heavy data parsing (which causes stuttering on lower-end devices). Forces us to ship massive class prototypes to the client, bloating the bundle size. Conflicts with SSR because classes passed from Astro server to Lit clients will still be degraded to plain objects unless re-hydrated *again* on the client.

### Option 3: The Hybrid Proxy Approach
Instead of deep-instantiating classes, wrap the root API responses in a JavaScript `Proxy`. The Proxy intercepts property access (like `.city` or `.equalsTo`) and dynamically routes it to a utility function behind the scenes.

**Considerations:**
- **Pros:** Keeps the UI component syntax clean and identical to legacy (`myLocation.city`) without the massive CPU overhead of deep recursive deserialization.
- **Cons:** Proxies introduce a slight runtime performance penalty on every property access. It relies on "magic" which can be difficult to debug and breaks TypeScript intellisense unless the interfaces are perfectly mapped to the Proxy handlers.

---

## Next Steps
Given the number of iterations required to settle on an optimal solution, it is highly recommended to audit the legacy `model` directory and inventory exactly how many methods/getters are actually used by the modernized UI. If the list is small (e.g., just `Address.equalsTo` and `Location.city`), **Option 1** is trivial to implement. If the codebase relies on hundreds of complex OOP traversals, **Option 2** might be a necessary bridging step despite the performance costs.
