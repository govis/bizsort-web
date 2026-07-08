import { LitElement, html, css } from 'lit';

import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';

import { Product } from '../navigation';

// Sub-components
import '../components/search/home';
import '../components/product/featured';

/**
 * Product Home page.
 * Ported from legacy product/home.ts.
 */
export class ProductHome extends LitElement {
  static get properties() {
    return {
      _narrow: { state: true }
    };
  }

  declare private _narrow: boolean;
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

  private _handleSearch(e: CustomEvent) {
    const { category, location, query, near, transactionType } = e.detail as any;
    
    if (category || (query && query.trim() !== '')) {
      Product.search(transactionType, category, query, location, near);
      return;
    }

    const featured = this.shadowRoot?.querySelector('product-featured') as any;
    if (featured) {
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
      /* Background image — matching legacy product-home-bg.jpg */
      background:
        linear-gradient(180deg, rgba(25,32,72,0.7) 0%, rgba(30,60,120,0.5) 40%, rgba(25,32,72,0.8) 100%),
        linear-gradient(135deg, #1a237e 0%, #283593 25%, #1565c0 50%, #0d47a1 75%, #1a237e 100%);
      background-size: cover;
      background-position: center;
      position: relative;
      font-family: Roboto, var(--wa-font-sans, sans-serif);
    }

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

    .menu-area {
      position: absolute;
      top: 1rem;
      right: 1rem;
      z-index: 20;
    }

    .menu-area wa-button::part(base) {
      color: rgba(255,255,255,0.9);
    }

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
        <search-home ?narrow="${this._narrow}" tab="product" @search-submit="${this._handleSearch}">
        </search-home>
      </div>

      <div class="spacer-mid"></div>

      <div class="featured-area">
        <product-featured></product-featured>
      </div>

      <div class="spacer-bottom"></div>
    `;
  }
}

if (!customElements.get('product-home')) {
  customElements.define('product-home', ProductHome);
}
