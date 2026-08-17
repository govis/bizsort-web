'use client';

import dynamic from 'next/dynamic';
import { ReactNode } from 'react';

const CompanyLayoutWrapper = dynamic(
  () => import('@/company/bundle').then((mod) => mod.CompanyLayoutWrapper),
  { ssr: false }
);

export default function CompanyLayoutClient({
  companyId,
  company,
  children
}: {
  companyId: number;
  company: any;
  children: ReactNode;
}) {
  return <CompanyLayoutWrapper companyId={companyId} company={company}>{children}</CompanyLayoutWrapper>;
}
