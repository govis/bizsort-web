'use client';

import React, { useEffect, useState } from 'react';
import { useRouter, useSelectedLayoutSegment } from 'next/navigation';
import type { Company } from '@/components/types';
import './header-layout';

// Import WebAwesome components used for tabs
import '@awesome.me/webawesome/dist/components/tab-group/tab-group.js';
import '@awesome.me/webawesome/dist/components/tab/tab.js';

export default function CompanyLayoutWrapper({
  companyId,
  children
}: {
  companyId: number;
  children: React.ReactNode;
}) {
  const router = useRouter();
  const segment = useSelectedLayoutSegment() || 'profile'; // 'products', 'jobs', etc.
  const [company, setCompany] = useState<Company | null>(null);

  useEffect(() => {
    async function fetchCompany() {
      try {
        const backendUrl = process.env.NEXT_PUBLIC_API_URL || '';
        const res = await fetch(`${backendUrl}/api/company/profile/view?company=${companyId}`);
        if (res.ok) {
          const data = await res.json();
          setCompany(data);
        }
      } catch (err) {
        console.error('Failed to load company for layout:', err);
      }
    }
    fetchCompany();
  }, [companyId]);

  if (!company) {
    return <div style={{ padding: '2rem', textAlign: 'center' }}>Loading company...</div>;
  }

  const logoUrl = company.image?.imageId 
    ? `${process.env.NEXT_PUBLIC_API_URL || ''}/api/image/get?entity=${company.image.entity}&id=${company.image.imageId}&maxImageSize=4`
    : '';

  // Handle Tab Navigation (acting like the bundle router)
  const handleTabChange = (e: any) => {
    const tabName = e.target.getAttribute('panel');
    if (tabName === 'profile') {
      router.push(`/company/${companyId}`);
    } else {
      router.push(`/company/${companyId}/${tabName}`);
    }
  };

  return (
    <company-header-layout
      title-text={company.name}
    >
      <div slot="logo" style={{ width: '100%', height: '100%' }}>
        {logoUrl ? (
          <img src={logoUrl} alt={`${company.name} logo`} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
        ) : (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
            {company.name.substring(0, 2).toUpperCase()}
          </div>
        )}
      </div>

      <div slot="tabs">
        <wa-tab-group onWaTabShow={handleTabChange}>
          <wa-tab slot="nav" panel="profile" active={segment === 'profile' ? true : undefined}>About</wa-tab>
          
          {company.offerings?.view && (
            <wa-tab slot="nav" panel="products" active={segment === 'products' ? true : undefined}>
              {company.offerings.label || 'What we Do'}
            </wa-tab>
          )}
          {company.projects && (
            <wa-tab slot="nav" panel="projects" active={segment === 'projects' ? true : undefined}>
              {company.projects.label || 'Projects'}
            </wa-tab>
          )}
          {company.jobs && (
            <wa-tab slot="nav" panel="jobs" active={segment === 'jobs' ? true : undefined}>
              {company.jobs.label || 'Jobs'}
            </wa-tab>
          )}
          {company.news && (
            <wa-tab slot="nav" panel="news" active={segment === 'news' ? true : undefined}>
              {company.news.label || 'News'}
            </wa-tab>
          )}
          {company.articles && (
            <wa-tab slot="nav" panel="articles" active={segment === 'articles' ? true : undefined}>
              {company.articles.label || 'Articles'}
            </wa-tab>
          )}
        </wa-tab-group>
      </div>

      {children}
    </company-header-layout>
  );
}
