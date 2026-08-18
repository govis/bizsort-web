import { html, css } from 'lit';
import type { ProductPreview } from '../types.js';
import { toPreview } from '../../service/product';
import { Product } from '../../navigation';
import { ListSlider } from '../list/slider';
import './card';

export class ProductSlider extends ListSlider<ProductPreview> {
  static get properties() {
    return {
      ...super.properties,
      productRefs: { type: Array, attribute: false }
    };
  }

  declare productRefs?: any[];
  declare private _displayOptions: any;

  constructor() {
    super();
    this._displayOptions = { company: false };
  }

  connectedCallback() {
    super.connectedCallback();
    this.fetchPage();
  }

  willUpdate(changed: Map<string, unknown>) {
    if (changed.has('productRefs') && changed.get('productRefs') !== undefined) {
      this.reset();
      this.fetchPage();
    }
  }

  protected async fetchPage() {
    if (!this.productRefs || this.productRefs.length === 0) return;
    this._loading = true;
    try {
      const previews = await toPreview(this.productRefs);
      this._items = [...this._items, ...previews];
    } catch (e) {
      console.error('Slider products error:', e);
    } finally {
      this._loading = false;
    }
  }

  private _handleProductSelect(e: CustomEvent<{ id: number; name: string }>) {
    if (this.companyId) {
      Product.view(this.companyId, e.detail.id);
    } else {
      Product.profileView(e.detail.id);
    }
  }

  protected renderItem(item: ProductPreview) {
    return html`
      <product-card .model="${item}" @product-select="${this._handleProductSelect}"></product-card>
    `;
  }

  static styles = [
    ListSlider.styles,
    css`
      product-card {
        scroll-snap-align: start;
        flex-shrink: 0;
        animation: card-enter 500ms cubic-bezier(0.4, 0, 0.2, 1) both;
      }

      @keyframes card-enter {
        from {
          opacity: 0;
          transform: translateY(40px) scale(0.95);
        }
        to {
          opacity: 1;
          transform: translateY(0) scale(1);
        }
      }

      product-card:nth-child(4n + 1) { animation-delay: 0ms; }
      product-card:nth-child(4n + 2) { animation-delay: 75ms; }
      product-card:nth-child(4n + 3) { animation-delay: 150ms; }
      product-card:nth-child(4n + 4) { animation-delay: 225ms; }
    `
  ];
}

if (!customElements.get('product-slider')) {
  customElements.define('product-slider', ProductSlider);
}
