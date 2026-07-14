'use client';

import React from 'react';

import { useSearchParams } from 'next/navigation';

// -- 1. Lit Web Component Imports (Registers custom elements for this bundle) --
import './home';
import './search';

// -- 2. React Client Boundaries --

/**
 * Renders the Product Home (Featured Products) web component.
 */
export function HomeWrapper() {
  const searchParams = useSearchParams();
  const categoryId = searchParams.get('categoryId') ? parseInt(searchParams.get('categoryId')!) : undefined;
  const locationId = searchParams.get('locationId') ? parseInt(searchParams.get('locationId')!) : undefined;
  const searchQuery = searchParams.get('searchQuery') || undefined;
  const searchNear = searchParams.get('searchNear') || undefined;
  const transactionType = searchParams.get('transactionType') ? parseInt(searchParams.get('transactionType')!) : undefined;

  return (
    // @ts-expect-error
    <product-home category-id={categoryId} location-id={locationId} search-query={searchQuery} search-near={searchNear} transaction-type={transactionType}></product-home>
  );
}
