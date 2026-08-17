import React from 'react';

declare global {
  namespace JSX {
    interface IntrinsicElements {
      'company-home': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'company-profile': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement> & { 'company-id'?: number, 'active-tab'?: string }, HTMLElement>;
      'company-search': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement> & { 'query'?: string, 'category-id'?: number }, HTMLElement>;
      'company-header-layout': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement> & { 'title-text'?: string }, HTMLElement>;
      'search-box': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement> & { 'query'?: string }, HTMLElement>;
      'page-menu': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement> & { 'theme'?: string }, HTMLElement>;
      'wa-tab-group': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'wa-tab': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement> & { slot?: string, panel?: string, active?: boolean | string }, HTMLElement>;
      'wa-dropdown': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'wa-button': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement> & { slot?: string, variant?: string, 'is-icon-button'?: boolean }, HTMLElement>;
      'wa-dropdown-item': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'wa-icon': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement> & { name?: string, slot?: string }, HTMLElement>;
    }
  }
}
