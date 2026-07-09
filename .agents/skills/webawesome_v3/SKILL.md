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
- In `<wa-button>`, the icon slots are `slot="start"` and `slot="end"`. **DO NOT** use `prefix` or `suffix`.

## 4. System Icons and CDN Defaults

- WebAwesome v3 ships with FontAwesome 7 and relies on a specific Alpha folder structure at `ka-f.fontawesome.com`.
- Do not attempt to override `setIconPath()` to point to standard CDNs (like jsdelivr) unless you know what you're doing, as this will break internal component icon rendering.
- For WebAwesome's internal bundled icons (like `check` or `minus`), always explicitly set the library: `<wa-icon name="check" library="system"></wa-icon>`.

## 5. Styling & CSS Variables (Strict Rules)

- WebAwesome manages interaction states (hover, active, focus) heavily using `color-mix()` on its internal CSS variables.
- **NEVER** manually target internal shadow parts (like `::part(base)`) to overwrite core properties like `background-color`, `color`, `width`, `height`, `padding`, or `border-radius`. 
  - Doing this for colors permanently breaks the component's interactive states. Use variables like `--wa-color-neutral-fill-loud` instead of `background-color`.
  - Doing this for sizing or radius breaks the component's internal geometry and padding logic.
- **ALWAYS** inspect the WebAwesome source code for the specific internal CSS variables (e.g., `--wa-color-neutral-fill-loud`, `--wa-color-fill-normal`) and override those variables on the host element or via `::slotted()` rules to ensure state logic remains intact.

## 6. `<wa-tag>` and Removable/Closable Attributes

- To make a `<wa-tag>` display an 'x' close button, use the **`with-remove`** attribute (not `removable` or `closable` as seen in older versions or other frameworks).
- When `with-remove` is present, it will fire the `@wa-remove` custom event when the close button is clicked.

## 7. `<wa-input>` Deep Theming

- WebAwesome dynamically changes which variables it listens to based on its `appearance` attribute (filled, outlined, default). Overriding just `--wa-form-control-background-color` will not pierce the shadow DOM if it falls back to a neutral palette variant!
- To fully customize a `<wa-input>`'s text, borders, placeholders, and backgrounds reliably across all appearances, you must override both the form control variables AND the underlying neutral palette variables simultaneously:
```css
wa-input {
    /* Backgrounds */
    --wa-form-control-background-color: transparent;
    --wa-color-neutral-fill-quiet: transparent;
    
    /* Text & Placeholders */
    --wa-form-control-value-color: var(--your-color);
    --wa-form-control-placeholder-color: var(--your-color);
    --wa-color-neutral-on-quiet: var(--your-color); /* Used for slotted icons */
    
    /* Borders */
    --wa-form-control-border-color: var(--your-color);
}
```

## 8. `<wa-button>` Variant Renaming (Shoelace v2 -> WebAwesome v3)

- Shoelace's `primary` and `default` variants have been strictly renamed to `brand` and `neutral` respectively.
- If you pass an invalid legacy variant (e.g., `variant="primary"`), the WebAwesome component will fail to apply its internal CSS selectors, causing it to lose its `border-radius`, paddings, and background colors, effectively rendering as an unstyled dark square!
- Valid button variants are: `neutral`, `brand`, `success`, `warning`, `danger`.

## 9. Verification Requirement

- **ALWAYS** check the latest WebAwesome v3 online documentation and inspect the current library's source code (e.g., `node_modules/@awesome.me/webawesome/dist/components/**/*.d.ts`) when customizing components.
- Do not blindly assume legacy Shoelace v2 configurations (like variants, slots, attributes, or CSS variables) still exist. You must verify proper configuration hooks to prevent silent failures.

## 10. `<wa-button is-icon-button>` Sizing and Shapes

- **Sizing:** WebAwesome form controls (including buttons) natively use `size="medium"` by default, which maps to `--wa-input-height-medium` (computing exactly to `40px` on a standard 16px root font). Do **NOT** manually override `width`, `height`, or `padding` inside `::part(base)` to size buttons. Rely on the native `size="..."` attribute properties or adjust the standard CSS custom variables for height.
- **Shapes:** To make an icon button perfectly circular, combine the `is-icon-button` attribute with the `pill` attribute (`<wa-button is-icon-button pill>`). **NEVER** set `border-radius: 50%` on `::part(base)` manually.
- **Shadows:** If you need to apply a `box-shadow` to a circular button, apply it directly to the `<wa-button>` host element in your CSS, but be sure to also apply `border-radius: 50%` to the host element, because the host remains a square block even if the internal `pill` base is rounded.
- **Deprecated `<wa-icon-button>`:** Shoelace v2 had a dedicated `<sl-icon-button>` component (which one might assume became `<wa-icon-button>`). This was completely dropped in WebAwesome v3. You must use `<wa-button is-icon-button><wa-icon></wa-icon></wa-button>` instead.

## 11. `<wa-tab-group>` Indicator Sizing

- **Track Width Units:** When customizing the active tab indicator line thickness (e.g., `--track-width`), **always use `rem` units** (e.g., `--track-width: 0.15rem;`) instead of `px` (like `2px`). WebAwesome's internal JavaScript engine calculates the dynamic active indicator width on render. Passing raw pixel strings can break its internal parser, causing it to fall back to `1px` after the initial render cycle.
- **Cascading Variables:** You do not need `!important` or hacky overrides for variables like `--safe-track-width`. Apply variables directly to the `<wa-tab-group>` host element, and they will cascade into the shadow DOM correctly.
