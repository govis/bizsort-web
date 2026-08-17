import { ReactNode } from 'react';
import CompanyLayoutClient from './CompanyLayoutClient';
import { view } from '@/service/company';

export default async function CompanyBundleLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ id: string }>;
}) {
  const resolvedParams = await params;
  const companyId = parseInt(resolvedParams.id, 10);
  const company = await view(companyId);

  return (
    <div className="company-bundle">
      <CompanyLayoutClient companyId={companyId} company={company}>
        {children}
      </CompanyLayoutClient>
    </div>
  );
}
