'use client';

import { setIconPath } from '@awesome.me/webawesome/dist/utilities/base-path.js';

if (typeof window !== 'undefined') {
  // Configure WebAwesome to fetch FontAwesome SVGs from jsdelivr CDN 
  // since the default ka-f.fontawesome.com endpoint may be blocked or require a kit token.
  setIconPath('https://cdn.jsdelivr.net/npm/@fortawesome/fontawesome-free@6.5.1/svgs');
}

export function WebAwesomeInit() {
  return null;
}
