import { LitElement, html, css } from 'lit';
import { unsafeHTML } from 'lit/directives/unsafe-html.js';

import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/spinner/spinner.js';

// Sub-components
import '../components/image/view';
import '../components/richtext/view';
import './header-layout';
import '../components/layout/card';

/**
 * Company Product View.
 * Ported from legacy company/product.ts.
 */
export class CompanyProduct extends LitElement {
  static get properties() {
    return {
      companyId: { type: Number, attribute: 'company-id' },
      productId: { type: Number, attribute: 'product-id' },
      _company: { state: true },
      _product: { state: true },
      _loading: { state: true },
      _error: { state: true }
    };
  }

  declare companyId?: number;
  declare productId?: number;
  declare private _company?: any;
  declare private _product?: any;
  declare private _loading: boolean;
  declare private _error?: string;

  constructor() {
    super();
    this._loading = false;
  }

  willUpdate(changedProperties: Map<string | number | symbol, unknown>) {
    if ((changedProperties.has('companyId') || changedProperties.has('productId')) && this.companyId && this.productId) {
      this._fetchData();
    }
  }

  private async _fetchData() {
    this._loading = true;
    this._error = undefined;
    try {
      const backendUrl = process.env.NEXT_PUBLIC_API_URL || '';
      
      // Fetch both company and product
      const [companyRes, productRes] = await Promise.all([
        fetch(`${backendUrl}/api/company/profile/view?company=${this.companyId}`),
        fetch(`${backendUrl}/api/product/profile/view?product=${this.productId}`)
      ]);
      
      if (!companyRes.ok) throw new Error('Failed to fetch company');
      if (!productRes.ok) throw new Error('Failed to fetch product');
      
      this._company = await companyRes.json();
      this._product = await productRes.json();
    } catch (e: unknown) {
      this._error = e instanceof Error ? e.message : 'An unknown error occurred';
    } finally {
      this._loading = false;
    }
  }

  // Image logic moved to image-view component

  static styles = css`
    :host {
      display: block;
      font-family: Roboto, var(--wa-font-sans, sans-serif);
      background-color: #f5f5f5;
      min-height: 100vh;
      color: #333;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 4rem;
    }

    .content-center {
      width: 100%;
      max-width: 800px;
      margin-left: auto;
      margin-right: auto;
    }

    .content {
      margin-top: 25px;
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    #mainCard {
      background-color: white;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.12);
      overflow: hidden;
    }

    .content-responsive {
      display: flex;
      flex-direction: column;
    }

    @media (min-width: 768px) {
      .content-responsive {
        flex-direction: row;
      }
      .image-section,
      .name-section {
        width: 50%;
      }
    }

    .image-section {
      background-color: #e0e0e0;
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 300px;
    }

    .image-section img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .name-section {
      background-color: #37474f; /* paper-blue-grey-800 */
      color: #fff;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .product-name {
      font-weight: 500;
      margin: 0;
      font-size: 24px;
    }

    .link-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .link-item {
      display: flex;
      align-items: center;
      gap: 12px;
      color: white;
      text-decoration: none;
      font-size: 16px;
    }

    .link-item wa-icon {
      font-size: 20px;
      color: rgba(255, 255, 255, 0.7);
    }

    .link-item:hover {
      text-decoration: underline;
    }

    .rich-text {
      padding: 16px;
      line-height: 1.5;
    }

    @keyframes slide-from-bottom {
      from {
        opacity: 0;
        transform: translateY(30px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    #mainCard {
      animation: slide-from-bottom 500ms cubic-bezier(0.4, 0, 0.2, 1) 200ms both;
    }

    #aboutCard {
      animation: slide-from-bottom 500ms cubic-bezier(0.4, 0, 0.2, 1) 400ms both;
    }
  `;

  render() {
    if (this._loading) {
      return html`
        <company-header-layout tab="product">
          <div class="loading-container">
            <wa-spinner style="font-size: 3rem;"></wa-spinner>
          </div>
        </company-header-layout>
      `;
    }

    if (this._error) {
      return html`
        <company-header-layout tab="product">
          <div style="max-width:1000px; margin: 2rem auto; color: red;">Error: ${this._error}</div>
        </company-header-layout>
      `;
    }

    if (!this._company || !this._product) {
      return html`
        <company-header-layout tab="product">
          <div style="max-width:1000px; margin: 2rem auto;">Data not found.</div>
        </company-header-layout>
      `;
    }

    const images = this._product?.images || [];

    return html`
      <company-header-layout tab="product">
        <div id="contentWidth" class="content content-center">
          
          <div id="mainCard">
            <div class="content-responsive">
              <div class="image-section">
                ${images.length > 0 ? html`<image-view .images="${images}" alt="${this._product.title}"></image-view>` : html`
                  <wa-icon name="image" style="font-size: 4rem; color: #999;"></wa-icon>
                `}
              </div>
              <div class="name-section">
                <h1 class="product-name">${this._product.title}</h1>
                
                <div class="link-list">
                  ${this._product.webUrl ? html`
                    <a class="link-item" href="${this._product.webUrl}" target="_blank" rel="noopener">
                      <wa-icon name="box-arrow-up-right"></wa-icon>
                      <span>Web page</span>
                    </a>
                  ` : ''}
                  
                  ${this._product.category ? html`
                    <a class="link-item" href="/search?categoryId=${this._product.category.id}">
                      <wa-icon name="folder"></wa-icon>
                      <span>${this._product.category.name}</span>
                    </a>
                  ` : ''}
                </div>
              </div>
            </div>
          </div>

          <layout-card id="aboutCard" heading="${this._product.type?.itemText || 'Product'} Description">
            <div class="rich-text">
              ${this._product.richText ? html`<richtext-view .html="${this._product.richText}"></richtext-view>` : this._product.text || 'No description available.'}
            </div>
          </layout-card>

        </div>
      </company-header-layout>
    `;
  }
}

if (!customElements.get('company-product')) {
  customElements.define('company-product', CompanyProduct);
}
