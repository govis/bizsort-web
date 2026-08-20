# BizSort Frontend (Astro)

This is the official modernized frontend for the BizSort platform. It utilizes Astro as the routing and Server-Side Rendering (SSR) layer, and Lit Web Components with Web Awesome (@awesome.me/webawesome) for the interactive UI.

## Architecture Guidelines

Before contributing, you **MUST** read the following architectural documents located in the .agents/docs/ directory of the repository root:
- ASTRO_SSR_ARCHITECTURE.md: Critical rules for hydrating Lit components with JSON payloads from Astro without causing Blank Pages.
- COMPANY_PAGES_ARCHITECTURE.md: Explanation of @lit/context usage across the layout shell.
- FRONTEND_ARCHITECTURE.md: General MVVM separation of concerns, native CSS animations, and service-layer abstraction rules.
- SPA_MODERNIZATION.md: Overview of the paradigm shift from the legacy SPA to the Astro MPA.

## Scripts

- 
pm run dev: Starts the Vite development server on http://localhost:4321. **Includes the /api/ proxy to the C# Backend.**
- 
pm run build: Compiles the Astro project into a standalone Node.js production application in dist/.
- 
pm run preview: Previews the built production application.
