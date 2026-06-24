import { ReactNode } from 'react';
import CompanyLayoutClient from './CompanyLayoutClient';

export default async function CompanyBundleLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ id: string }>;
}) {
  const resolvedParams = await params;
  const companyId = parseInt(resolvedParams.id, 10);

  return (
    <div className="company-bundle">
      <CompanyLayoutClient companyId={companyId}>
        {children}
      </CompanyLayoutClient>
    </div>
  );
}
