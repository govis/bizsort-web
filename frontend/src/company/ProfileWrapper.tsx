'use client';

import React from 'react';
import './profile';

interface CompanyProfileProps {
  companyId: number;
}

export default function CompanyProfileWrapper({ companyId }: CompanyProfileProps) {
  return <company-profile company-id={companyId}></company-profile>;
}
