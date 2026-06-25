'use client';

import React, { useEffect, useState, useRef } from 'react';
import { useRouter, useSelectedLayoutSegment } from 'next/navigation';
import type { Company } from '@/components/types';
import { view } from '../service/company';

// -- 1. Lit Web Component Imports (Registers custom elements for this bundle) --
import './home';
import './profile';
import './header-layout';
import '@awesome.me/webawesome/dist/components/tab-group/tab-group.js';
import '@awesome.me/webawesome/dist/components/tab/tab.js';
import '../components/menu/page';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';

// -- 2. React Client Boundaries --

/**
 * Renders the Company Home (Featured Companies) web component.
 */
export function HomeWrapper() {
  return <company-home></company-home>;
}

/**
 * Renders the specific Company Profile web component.
 */
export function CompanyProfileWrapper({ companyId, activeTab = 'about' }: { companyId: number, activeTab?: string }) {
  return <company-profile company-id={companyId} active-tab={activeTab}></company-profile>;
}

/**
 * Renders the shared Layout wrapper (Header, Logo, Tabs) for a Company.
 */
export function CompanyLayoutWrapper({
  companyId,
  children
}: {
  companyId: number;
  children: React.ReactNode;
}) {
  const router = useRouter();
  const segment = useSelectedLayoutSegment() || 'profile';
  const [company, setCompany] = useState<Company | null>(null);
  const tabGroupRef = useRef<HTMLElement>(null);

  useEffect(() => {
    async function fetchCompany() {
      try {
        const data = await view(companyId);
        setCompany(data);
      } catch (err) {
        console.error('Failed to load company for layout:', err);
      }
    }
    fetchCompany();
  }, [companyId]);

  const handleTabChange = (e: any) => {
    const tabName = e.target.getAttribute('panel');
    if (tabName === 'profile') {
      router.push(`/company/${companyId}`);
    } else {
      router.push(`/company/${companyId}/${tabName}`);
    }
  };

  useEffect(() => {
    const tabGroup = tabGroupRef.current;
    if (tabGroup) {
      tabGroup.addEventListener('wa-tab-show', handleTabChange);
      return () => tabGroup.removeEventListener('wa-tab-show', handleTabChange);
    }
  }, [companyId, router]);

  if (!company) {
    return <div style={{ padding: '2rem', textAlign: 'center' }}>Loading company...</div>;
  }

  const logoUrl = company.image?.imageId 
    ? `${process.env.NEXT_PUBLIC_API_URL || ''}/api/image/get?entity=${company.image.entity}&id=${company.image.imageId}&maxImageSize=4`
    : '';



  return (
    <company-header-layout title-text={company.name}>
      <div slot="logo" style={{ width: '100%', height: '100%' }}>
        {logoUrl ? (
          <img src={logoUrl} alt={`${company.name} logo`} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
        ) : (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
            {company.name.substring(0, 2).toUpperCase()}
          </div>
        )}
      </div>

      <wa-button slot="navbar" variant="text" style={{ fontSize: '1.5rem', color: 'white' }}>
        <wa-icon name="search"></wa-icon>
      </wa-button>

      <page-menu slot="dropdown" theme="dark">
        <wa-dropdown-item>Directory</wa-dropdown-item>
        <wa-dropdown-item>Enrichment</wa-dropdown-item>
        <wa-dropdown-item>Share Community</wa-dropdown-item>
      </page-menu>

      <div slot="tabs">
        <wa-tab-group ref={tabGroupRef}>
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
