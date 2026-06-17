'use client';

import React, { useEffect, useRef } from 'react';
import '@/components/company-profile';

interface CompanyProfileProps {
  companyId: number;
}

declare global {
  namespace JSX {
    interface IntrinsicElements {
      'company-profile': any;
    }
  }
}

export default function CompanyProfileWrapper({ companyId }: CompanyProfileProps) {
  return <company-profile company-id={companyId}></company-profile>;
}
