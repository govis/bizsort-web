import type { Metadata } from 'next';
import CompanyProfilePage from './CompanyProfilePage';

export const metadata: Metadata = {
  title: 'Company Profile',
  description: 'View detailed company profile including contact information, products, services, and more.',
};

export default function CompanyPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  return <CompanyProfilePage params={params} />;
}
