'use client';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';
import { registerIcons } from '../../icons';

export default function NavigationProvider() {
  const router = useRouter();

  useEffect(() => {
    registerIcons();
    const handleNavigate = (e: any) => {
      if (e.detail?.url) {
        router.push(e.detail.url);
      }
    };
    
    window.addEventListener('app-navigate', handleNavigate);
    return () => window.removeEventListener('app-navigate', handleNavigate);
  }, [router]);

  return null;
}
