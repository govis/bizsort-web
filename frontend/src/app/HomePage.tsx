'use client';

import dynamic from 'next/dynamic';

const HomeWrapper = dynamic(
  () => import('@/company/HomeWrapper'),
  { ssr: false }
);

export default function HomePage() {
  return (
    <main style={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <HomeWrapper />
    </main>
  );
}
