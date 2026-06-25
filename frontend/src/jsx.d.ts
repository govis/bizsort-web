import 'react';

declare module 'react' {
  namespace JSX {
    interface IntrinsicElements {
      'company-profile': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { 'company-id'?: number, 'active-tab'?: string };
      'company-home': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'company-header-layout': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { 'title-text'?: string };
      'wa-tab-group': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { onWaTabShow?: (e: any) => void };
      'wa-tab': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { panel?: string, active?: boolean };
      'wa-button': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { variant?: string, 'is-icon-button'?: boolean };
      'wa-icon': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { name?: string };
      'page-menu': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { theme?: string };
      'wa-dropdown-item': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
    }
  }
}
