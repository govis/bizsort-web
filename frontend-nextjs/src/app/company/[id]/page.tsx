import type { Metadata } from 'next';
import CompanyProfilePage from './CompanyProfilePage';
import { view } from '@/service/company';

export async function generateMetadata({
  params,
}: {
  params: Promise<{ id: string }>;
}): Promise<Metadata> {
  const resolvedParams = await params;
  const companyId = parseInt(resolvedParams.id, 10);
  const company = await view(companyId);

  const title = company.category 
    ? `${company.name} - ${company.category.name} | BizSort`
    : `${company.name} | BizSort`;

  return {
    title,
    description: company.text || `View detailed company profile for ${company.name}.`,
    alternates: {
      canonical: `/company/${companyId}`,
    }
  };
}

export default async function CompanyPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ tab?: string }>;
}) {
  const resolvedParams = await params;
  const resolvedSearchParams = await searchParams;
  const companyId = parseInt(resolvedParams.id, 10);
  const company = await view(companyId);
  const activeTab = resolvedSearchParams.tab || 'about';

  // Generate JSON-LD Structured Data
  const jsonLd: any = {
    '@context': 'https://schema.org',
    '@type': 'LocalBusiness',
    name: company.name,
    address: {
      '@type': 'PostalAddress',
      streetAddress: company.headOffice?.location?.address || '',
    },
  };

  if (company.headOffice?.location?.geoLocation) {
    jsonLd.geo = {
      '@type': 'GeoCoordinates',
      latitude: company.headOffice.location.geoLocation.lat,
      longitude: company.headOffice.location.geoLocation.lng,
    };
  }

  if (company.headOffice?.phone) {
    jsonLd.telephone = company.headOffice.phone;
  }

  if (company.image?.imageId) {
    const backendUrl = process.env.NEXT_PUBLIC_API_URL || '';
    jsonLd.logo = `${backendUrl}/api/image/get?entity=${company.image.entity}&id=${company.image.imageId}`;
  }

  if (company.webSite) {
    jsonLd.url = company.webSite;
  }

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
      <CompanyProfilePage companyId={companyId} company={company} activeTab={activeTab} />
    </>
  );
}
