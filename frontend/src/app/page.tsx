import type { Metadata } from 'next';
import HomePage from './HomePage';

export const metadata: Metadata = {
  title: 'BizSort - Business Directory',
  description: 'Find and explore local businesses, view company profiles, products, services, and more. Search by category, location, and industry.',
  keywords: ['business directory', 'company profiles', 'local business', 'products', 'services', 'B2B', 'B2C'],
  openGraph: {
    title: 'BizSort - Business Directory',
    description: 'Find and explore local businesses, view company profiles, products, services, and more.',
    type: 'website',
  },
};

export default function Home() {
  return <HomePage />;
}
