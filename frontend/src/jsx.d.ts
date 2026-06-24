import 'react';

declare module 'react' {
  namespace JSX {
    interface IntrinsicElements {
      'company-profile': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { 'company-id'?: number, 'active-tab'?: string };
      'company-home': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement>;
      'company-header-layout': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { 'title-text'?: string };
      'wa-tab-group': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { onWaTabShow?: (e: any) => void };
      'wa-tab': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { panel?: string, active?: boolean };
    }
  }
}

