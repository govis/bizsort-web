import { LitElement, html, css } from 'lit';

import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';

import { Company } from '../navigation';

// Sub-components
import '../components/search/home';
import '../components/search/options';
import '../components/company/featured';

/**
 * Home page — the landing page for BizSort.
 * Ported from legacy company/home.ts.
 *
 * Legacy used: connect(store)(PageElement), Redux state for responsive width,
 * PageModel/ViewModel for SEO and search validation.
 * Modern: Standalone Lit element, responsive via CSS media queries,
 * SEO handled by Next.js metadata exports, routing by App Router.
 */
export class CompanyHome extends LitElement {
  static get properties() {
    return {
      _narrow: { state: true },
      categoryId: { type: Number, attribute: 'category-id' },
      locationId: { type: Number, attribute: 'location-id' },
      searchQuery: { type: String, attribute: 'search-query' },
      searchNear: { type: String, attribute: 'search-near' },
      transactionType: { type: Number, attribute: 'transaction-type' }
    };
  }

  declare private _narrow: boolean;
  declare categoryId?: number;
  declare locationId?: number;
  declare searchQuery?: string;
  declare searchNear?: string;
  declare transactionType?: number;

  private _searchOptions = [
    { value: 1, text: "Business", selected: true },
    { value: 2, text: "Consumer", selected: true }
  ];
  private _resizeObserver?: ResizeObserver;

  constructor() {
    super();
    this._narrow = window.innerWidth < 640;
  }

  connectedCallback() {
    super.connectedCallback();
    this._resizeObserver = new ResizeObserver((entries) => {
      for (const entry of entries) {
        this._narrow = entry.contentRect.width < 640;
      }
    });
    this._resizeObserver.observe(this);
  }

  disconnectedCallback() {
    this._resizeObserver?.disconnect();
    super.disconnectedCallback();
  }

  willUpdate(changedProperties: Map<string, any>) {
    if (changedProperties.has('transactionType')) {
      if (this.transactionType !== undefined) {
        this._searchOptions[0].selected = (this.transactionType & 1) === 1;
        this._searchOptions[1].selected = (this.transactionType & 2) === 2;
      }
    }
  }

  private _handleSearch(e: CustomEvent) {
    const { category, location, query, near } = e.detail as any;
    const searchOptions = this.shadowRoot?.querySelector('search-options') as any;
    const tType = searchOptions ? searchOptions.selectedValues() : (this.transactionType || 3);
    
    // If a specific category or search query is provided, navigate to the search page
    if (category || (query && query.trim() !== '')) {
      Company.search(tType, category, query, location, near);
      return;
    }

    // Otherwise (empty search), behave like browse mode and update the featured section below
    const featured = this.shadowRoot?.querySelector('company-featured') as any;
    if (featured) {
      // Mirror legacy: search-home$._reflectSelection sets selection.location to
      // LocationSettings.country.id (1) if location is falsy/0
      featured.selection = {
        category: category ?? 0,
        location: location ? location : 1
      };
    }
  }

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
      /* Background image — matching legacy directory-home-bg.jpg */
      background:
        linear-gradient(180deg, rgba(25,32,72,0.7) 0%, rgba(30,60,120,0.5) 40%, rgba(25,32,72,0.8) 100%),
        linear-gradient(135deg, #1a237e 0%, #283593 25%, #1565c0 50%, #0d47a1 75%, #1a237e 100%);
      background-size: cover;
      background-position: center;
      position: relative;
      font-family: Roboto, var(--wa-font-sans, sans-serif);
    }

    /* Logo area */
    .logo-area {
      position: absolute;
      top: 1rem;
      left: 1rem;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      z-index: 20;
    }

    .logo-icon {
      width: 48px;
      height: 48px;
      background: rgba(255,255,255,0.15);
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: bold;
      font-size: 1rem;
      color: white;
    }

    .logo-icon span:last-child {
      color: #ffeb3b;
    }

    /* Menu area */
    .menu-area {
      position: absolute;
      top: 1rem;
      right: 1rem;
      z-index: 20;
    }

    .menu-area wa-button::part(base) {
      color: rgba(255,255,255,0.9);
    }

    /* Content layout */
    .spacer-top {
      flex: 2;
      min-height: 80px;
    }

    :host([narrow]) .spacer-top {
      flex: 1;
      min-height: 52px;
    }

    .search-area {
      display: flex;
      justify-content: center;
      padding: 0 1rem;
      z-index: 10;
    }

    .spacer-mid {
      min-height: 20px;
      flex: 0.5;
    }

    .featured-area {
      padding: 0 1rem;
      z-index: 10;
    }

    .spacer-bottom {
      flex: 1;
    }

    /* Entry animations */
    @keyframes fade-in-up {
      from {
        opacity: 0;
        transform: translateY(30px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .search-area {
      animation: fade-in-up 600ms cubic-bezier(0.4, 0, 0.2, 1) 200ms both;
    }

    .featured-area {
      animation: fade-in-up 600ms cubic-bezier(0.4, 0, 0.2, 1) 500ms both;
    }

    .logo-area {
      animation: fade-in-up 400ms cubic-bezier(0.4, 0, 0.2, 1) 100ms both;
    }
  `;

  render() {
    return html`
      <!-- Logo -->
      <div class="logo-area">
        <div class="logo-icon">
          <span>biz</span><span>SORT</span>
        </div>
      </div>

      <!-- Menu -->
      <div class="menu-area">
        <wa-dropdown>
          <wa-button slot="trigger" variant="text" is-icon-button>
            <wa-icon name="three-dots-vertical" style="color: white;"></wa-icon>
          </wa-button>
          <wa-dropdown-item>Contact Us</wa-dropdown-item>
          <wa-dropdown-item>Categories</wa-dropdown-item>
        </wa-dropdown>
      </div>

      <!-- Layout -->
      <div class="spacer-top"></div>

      <div class="search-area">
        <search-home 
          ?narrow="${this._narrow}" 
          @search-submit="${this._handleSearch}"
          category-id="${this.categoryId ?? 0}"
          location-id="${this.locationId ?? 0}"
          search-query="${this.searchQuery ?? ''}"
          search-near="${this.searchNear ?? ''}"
        >
          <search-options 
            .values="${this._searchOptions}"
          ></search-options>
        </search-home>
      </div>

      <div class="spacer-mid"></div>

      <div class="featured-area">
        <company-featured></company-featured>
      </div>

      <div class="spacer-bottom"></div>
    `;
  }
}

if (!customElements.get('company-home')) {
  customElements.define('company-home', CompanyHome);
}
