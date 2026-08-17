'use client';

import dynamic from 'next/dynamic';

const CompanyProfileWrapper = dynamic(
  () => import('@/company/bundle').then((mod) => mod.CompanyProfileWrapper),
  { ssr: false }
);

export default function CompanyProfilePage({
  companyId,
  company,
  activeTab
}: {
  companyId: number;
  company: any;
  activeTab: string;
}) {
  return (
    <main>
      <CompanyProfileWrapper companyId={companyId} company={company} activeTab={activeTab} />
    </main>
  );
}
