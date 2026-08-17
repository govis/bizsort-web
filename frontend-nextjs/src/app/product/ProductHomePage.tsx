'use client';

import dynamic from 'next/dynamic';

const HomeWrapper = dynamic(
  () => import('../../product/bundle').then((mod) => mod.HomeWrapper),
  { ssr: false }
);

export default function ProductHomePage() {
  return (
    <main style={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <HomeWrapper />
    </main>
  );
}
