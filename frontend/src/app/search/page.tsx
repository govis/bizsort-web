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
  const query = typeof resolvedParams.query === 'string' ? resolvedParams.query : undefined;
  const categoryId = typeof resolvedParams.category === 'string' ? parseInt(resolvedParams.category, 10) : undefined;

  return (
    <main>
      <CompanySearchWrapper query={query} categoryId={categoryId} />
    </main>
  );
}
