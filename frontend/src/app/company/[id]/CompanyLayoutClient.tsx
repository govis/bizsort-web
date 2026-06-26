'use client';

import dynamic from 'next/dynamic';
import { ReactNode } from 'react';

const CompanyLayoutWrapper = dynamic(
  () => import('@/company/bundle').then((mod) => mod.CompanyLayoutWrapper),
  { ssr: false }
);

export default function CompanyLayoutClient({
  companyId,
  children
}: {
  companyId: number;
  children: ReactNode;
}) {
  return <CompanyLayoutWrapper companyId={companyId}>{children}</CompanyLayoutWrapper>;
}
