'use client';

import { use } from 'react';
import dynamic from 'next/dynamic';

const CompanyProfileWrapper = dynamic(
  () => import('@/company/ProfileWrapper'),
  { ssr: false }
);

export default function CompanyProfilePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const companyId = parseInt(id, 10);

  return (
    <main>
      <CompanyProfileWrapper companyId={companyId} />
    </main>
  );
}
