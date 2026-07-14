import { Metadata } from 'next';
import { CompanySearchWrapper } from '@/company/bundle';

export const metadata: Metadata = {
  title: 'Search | BizSort',
  description: 'Search for companies and organizations in BizSort.',
};

export default async function SearchPage({
  searchParams,
}: {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}) {
  const resolvedParams = await searchParams;
  const query = typeof resolvedParams.searchQuery === 'string' ? resolvedParams.searchQuery : undefined;
  const categoryId = typeof resolvedParams.categoryId === 'string' ? parseInt(resolvedParams.categoryId, 10) : undefined;
  const locationId = typeof resolvedParams.locationId === 'string' ? parseInt(resolvedParams.locationId, 10) : undefined;
  const searchNear = typeof resolvedParams.searchNear === 'string' ? resolvedParams.searchNear : undefined;
  const transactionType = typeof resolvedParams.transactionType === 'string' ? parseInt(resolvedParams.transactionType, 10) : undefined;

  return (
    <main>
      <CompanySearchWrapper 
        query={query} 
        categoryId={categoryId} 
        locationId={locationId} 
        searchNear={searchNear} 
        transactionType={transactionType} 
      />
    </main>
  );
}
