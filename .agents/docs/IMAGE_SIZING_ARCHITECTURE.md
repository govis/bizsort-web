# Image Sizing & Fetch Architecture

This document tracks the authoritative legacy image dimension configurations for the BizSort platform, ensuring the Astro modernization maintains perfect visual parity with the original Polymer constraints.

## The Fetch Pipeline (`getLogoUrl`)

The modern frontend utilizes `src/service/image.ts` -> `getLogoUrl()` to interface with the C# backend.

Unlike modern CDNs which often return fluid, arbitrarily sized images, the BizSort legacy backend endpoint (`/api/image/get`) explicitly requires bounding box parameters (`width`, `height`) alongside the entity identifiers (`entity`, `id`).

**CRITICAL ENDPOINT BEHAVIOR**:
- The backend processes these dimensions as a **bounding box**, using `ResizeMode.Max`.
- It will resize the image to fit entirely *within* the requested box while perfectly preserving the intrinsic aspect ratio.
- If the requested dimensions are *larger* than the native image size, the backend will return the native image untouched.
- The ASP.NET Core `height` query parameter is `int?` (nullable). If `height` is omitted, the C# backend defaults to `0`, causing the scaling engine to only constrain by `width`.

## Image Sizing Contexts

To avoid magic numbers sprinkled throughout the codebase, all authoritative image dimensions have been centralized inside `frontend-astro/src/service/image.ts` as `ImageSizes`.

When constructing a UI component, you must consult these dimensions to ensure consistency:

### 1. Company
- **Card**: `240 x 120` (Used in search result grids)
- **List**: `200 x 100` (Used in list-view search results)
- **ViewHead**: `290 x 145` (Used as the main profile header logo, scaled dynamically by fluid CSS)

### 2. Offering (Legacy: Product)
- **Card**: `240 x 120`
- **List**: `120 x 120` (Square aspect ratio preferred for list thumbnails)
- **ViewHead**: `300 x 300` (Square constraints for the profile header)

### 3. Community
- **Card**: `240 x 120`
- **List**: `240 x 120`

### 4. Person
- **ViewHead**: `290 x 145`

### 5. News & Articles
- **Icon**: `90 x 90`

## Implementation Pattern

When building Astro or Lit components that fetch images:

1. Import `ImageEntity` and `ImageSizes` from `service/image.ts`.
2. Map the domain model to the `ImageEntity` enum.
3. Call `getLogoUrl(entity, id, width, height)`.

```typescript
import { getLogoUrl, ImageEntity, ImageSizes } from '../../service/image.js';

// Inside a Lit render() function or Astro script:
const dimensions = ImageSizes.Company.ViewHead;
const logoUrl = getLogoUrl(
    ImageEntity.Company, 
    this.company.image?.imageId, 
    dimensions.width, 
    dimensions.height
);
```

## Background Color Blending (`analyzeImage`)

For `Card` contexts (e.g., `company-card`), the legacy frontend analyzes the pixels around the border of the fetched image to calculate a dominant background color. This color is then applied dynamically via CSS variables (e.g. `--card-header-background`).

**Requirements for Color Extraction**:
- The `<img />` tag MUST include `crossOrigin="anonymous"`. Without this, Canvas 2D `getImageData` will throw a security exception ("Tainted canvas") because the image originated from the backend (`localhost:5001`) rather than the frontend (`localhost:4321`).
- The C# backend's `Program.cs` MUST configure the CORS policy with `AllowAnyOrigin()`.
- The `object-fit: contain` CSS rule MUST be used on the image to ensure the extracted edge pixels perfectly match the padding color.
