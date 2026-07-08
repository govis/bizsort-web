'use client';

import React from 'react';

// -- 1. Lit Web Component Imports (Registers custom elements for this bundle) --
import './home';
import './search';

// -- 2. React Client Boundaries --

/**
 * Renders the Product Home (Featured Products) web component.
 */
export function HomeWrapper() {
  return <product-home></product-home>;
}
