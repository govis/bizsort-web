---
name: WebAwesome v3 Guidelines
description: Guidelines, gotchas, and breaking changes when working with WebAwesome v3 (formerly Shoelace), including event names, popup vs dropdown usage, and styling via CSS variables.
---

# WebAwesome v3 (Shoelace) Usage Guide

When building UI components using WebAwesome v3 (the successor to Shoelace), you must adhere to the following rules and gotchas to prevent breaking interactions, styling issues, and event bugs.

## 1. Native DOM Events (No `wa-` prefix on standard events)

WebAwesome v3 dropped the framework prefix for standard DOM events on form controls. 
- **DO NOT** use `@wa-input`, `@wa-change`, `@wa-focus`, or `@wa-blur`. These will never fire!
- **DO** use the native event listeners instead: `@input`, `@change`, `@focus`, and `@blur`.
- *Note:* Custom component-specific events (like dropdown toggles) still use the prefix (e.g., `@wa-show`, `@wa-hide`).

## 2. Dropdown vs. Popup for Autocompletes

- **`<wa-dropdown>`**: This component acts as a strict Menu wrapper. When opened, it purposefully hijacks keyboard focus and shifts it to the first menu item for accessibility. 
  - **CRITICAL**: Never use `<wa-dropdown>` as a wrapper for typeahead/autocomplete inputs (`<wa-input>`), as it will steal focus while the user is trying to type and prematurely close the menu when they attempt to click back into the input.
- **`<wa-popup>`**: Use this component for custom floating elements like autocomplete suggestions. It acts as a robust wrapper around `floating-ui`, handling viewport collision detection and flipping without stealing focus.
  - Usage pattern: Pass the trigger element (e.g., `<wa-input>`) using `slot="anchor"`.
  - Configure properties like `sync="width"`, `flip`, and bind its visibility using `?active`.

## 3. Slot Naming Gotchas (Icons)

- WebAwesome renamed several core slots across its components.
- In `<wa-dropdown-item>`, the icon slot is now strictly `slot="icon"`. 
- **DO NOT** use `slot="prefix"` or `slot="start"` for icons in dropdown items; they will be swallowed by the Shadow DOM and fail to render.

## 4. System Icons and CDN Defaults

- WebAwesome v3 ships with FontAwesome 7 and relies on a specific Alpha folder structure at `ka-f.fontawesome.com`.
- Do not attempt to override `setIconPath()` to point to standard CDNs (like jsdelivr) unless you know what you're doing, as this will break internal component icon rendering.
- For WebAwesome's internal bundled icons, always explicitly set the library: `<wa-icon name="search" library="system"></wa-icon>`.

## 5. Styling & CSS Variables (Strict Rules)

- WebAwesome manages interaction states (hover, active, focus) heavily using `color-mix()` on its internal CSS variables.
- **NEVER** manually target internal shadow parts (like `::part(base)`) to overwrite core properties like `background-color` or `color`. This permanently breaks the component's interactive states.
- **ALWAYS** inspect the WebAwesome source code for the specific internal CSS variables (e.g., `--wa-color-neutral-fill-loud`, `--wa-color-fill-normal`) and override those variables on the host element or via `::slotted()` rules to ensure state logic remains intact.
