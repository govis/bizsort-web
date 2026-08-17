import type { Metadata } from 'next';
import ProductHomePage from './ProductHomePage';

export const metadata: Metadata = {
  title: 'BizSort - Offerings',
  description: 'Find and explore local business offerings, products, and services.',
};

export default function ProductHome() {
  return <ProductHomePage />;
}
