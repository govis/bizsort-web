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
import '../components/search/box';
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
    const tabName = e.detail.name;
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
          <img src={logoUrl} alt={`${company.name} logo`} style={{ width: '100%', height: '100%', objectFit: 'contain', backgroundColor: 'white' }} />
        ) : (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
            {company.name.substring(0, 2).toUpperCase()}
          </div>
        )}
      </div>

      <search-box slot="navbar"></search-box>

      <page-menu slot="dropdown" theme="dark">
        <wa-dropdown-item>
          <wa-icon slot="icon" name="pen"></wa-icon>
          Add your Company
        </wa-dropdown-item>
        <wa-dropdown-item>
          <wa-icon slot="icon" name="tag"></wa-icon>
          Tag this Company
        </wa-dropdown-item>
        <wa-dropdown-item>
          <wa-icon slot="icon" name="share-nodes"></wa-icon>
          Share with Community
        </wa-dropdown-item>
      </page-menu>

      <div slot="tabs">
        <wa-tab-group ref={tabGroupRef}>
          <wa-tab slot="nav" panel="profile" active={segment === 'profile' ? true : undefined}>About</wa-tab>
          
          {company.offerings?.view ? (
            <wa-tab slot="nav" panel="products" active={segment === 'products' ? true : undefined}>
              {company.offerings.label || 'What we Do'}
            </wa-tab>
          ) : null}
          {company.projects != null ? (
            <wa-tab slot="nav" panel="projects" active={segment === 'projects' ? true : undefined}>
              {company.projects.label || 'Projects'}
            </wa-tab>
          ) : null}
          {company.jobs != null ? (
            <wa-tab slot="nav" panel="jobs" active={segment === 'jobs' ? true : undefined}>
              {company.jobs.label || 'Jobs'}
            </wa-tab>
          ) : null}
          {company.news != null ? (
            <wa-tab slot="nav" panel="news" active={segment === 'news' ? true : undefined}>
              {company.news.label || 'News'}
            </wa-tab>
          ) : null}
          {company.articles != null ? (
            <wa-tab slot="nav" panel="articles" active={segment === 'articles' ? true : undefined}>
              {company.articles.label || 'Articles'}
            </wa-tab>
          ) : null}
        </wa-tab-group>
      </div>

      <style>{`
        wa-tab-group {
          --indicator-color: white;
          --track-color: transparent;
          width: 100%;
        }
        wa-tab {
          color: rgba(255, 255, 255, 0.7);
        }
        wa-tab::part(base) {
          padding: 0.75rem 1.5rem;
          font-weight: 500;
        }
        wa-tab[active] {
          color: white;
        }
        wa-button[slot="navbar"]::part(base) {
          color: white;
        }
      `}</style>

      {children}
    </company-header-layout>
  );
}
