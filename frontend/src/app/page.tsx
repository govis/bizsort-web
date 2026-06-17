'use client';

import dynamic from 'next/dynamic';

const CompanyProfileWrapper = dynamic(
  () => import('@/components/CompanyProfileWrapper'),
  { ssr: false }
);

export default function Home() {
  return (
    <main>
      <CompanyProfileWrapper companyId={1} />
    </main>
  );
}
