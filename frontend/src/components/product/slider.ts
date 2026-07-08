import { LitElement, html, css } from 'lit';
import { repeat } from 'lit/directives/repeat.js';
import type { ProductPreview } from '../types.js';
import { toPreview } from '../../service/product';
import './card';

import '@awesome.me/webawesome/dist/components/spinner/spinner.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/button/button.js';

export class ProductSlider extends LitElement {
  static get properties() {
    return {
      _items: { state: true },
      _loading: { state: true },
      _nextIndex: { state: true },
      companyId: { type: Number, attribute: 'company-id' },
      productRefs: { type: Array, attribute: false }
    };
  }

  declare companyId?: number;
  declare productRefs?: any[];

  declare private _items: ProductPreview[];
  declare private _loading: boolean;
  declare private _nextIndex: number;
  declare private _displayOptions: any;

  constructor() {
    super();
    this._items = [];
    this._loading = false;
    this._nextIndex = 0;
    this._displayOptions = { company: false };
  }

  connectedCallback() {
    super.connectedCallback();
    this._fetchProducts();
  }

  willUpdate(changed: Map<string, unknown>) {
    if (changed.has('productRefs') && changed.get('productRefs') !== undefined) {
      this._items = [];
      this._nextIndex = 0;
      this._fetchProducts();
    }
  }

  private async _fetchProducts() {
    if (!this.productRefs || this.productRefs.length === 0) return;
    this._loading = true;
    try {
      // Legacy ProductSlider mapped productRefs to preview models via toPreview
      // using the companyId context so the preview doesn't redundanty render company info
      const previews = await toPreview(this.productRefs);
      this._items = [...this._items, ...previews];
      // Note: slider usually shows all of them if productRefs is pre-populated
    } catch (e) {
      console.error('Slider products error:', e);
    } finally {
      this._loading = false;
    }
  }

  private _handleProductSelect(e: CustomEvent<{ id: number; name: string }>) {
    if (this.companyId) {
      window.location.href = \`/company/\${this.companyId}/product/\${e.detail.id}\`;
    } else {
      window.location.href = \`/product/\${e.detail.id}\`;
    }
  }

  static styles = css`
    :host {
      display: block;
      padding: 1rem 0;
    }

    .carousel {
      display: flex;
      gap: 1rem;
      overflow-x: auto;
      padding: 0.5rem;
      scroll-snap-type: x mandatory;
      scrollbar-width: thin;
    }

    .carousel::-webkit-scrollbar {
      height: 8px;
    }

    .carousel::-webkit-scrollbar-thumb {
      background: #ccc;
      border-radius: 4px;
    }

    product-card {
      scroll-snap-align: start;
      flex-shrink: 0;
    }

    .loading {
      display: flex;
      justify-content: center;
      padding: 2rem;
    }
  `;

  render() {
    return html`
      ${this._items.length > 0 ? html`
        <div class="carousel">
          ${repeat(this._items, (item) => item.id, (item) => html`
            <product-card .model="${item}" @product-select="${this._handleProductSelect}"></product-card>
          `)}
        </div>
      ` : ''}

      ${this._loading ? html`
        <div class="loading">
          <wa-spinner style="font-size: 2rem;"></wa-spinner>
        </div>
      ` : ''}
    `;
  }
}

if (!customElements.get('product-slider')) {
  customElements.define('product-slider', ProductSlider);
}
