import { LitElement, html, css } from 'lit';
import { repeat } from 'lit/directives/repeat.js';
import type { OfferingPreview } from '../types.js';
import { getFeatured, toPreview } from '../../service/offering';
import { Offering } from '../../navigation';
import './card';

import '@awesome.me/webawesome/dist/components/spinner/spinner.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/button/button.js';

export interface Selection {
  category: number;
  location: number;
}

const DEFAULT_LOCATION = 1;
const DEFAULT_CATEGORY = 0;

export class OfferingFeatured extends LitElement {
  static get properties() {
    return {
      _items: { state: true },
      _loading: { state: true },
      _nextIndex: { state: true },
      selection: { type: Object, attribute: false }
    };
  }

  declare selection: Selection;

  declare private _items: OfferingPreview[];
  declare private _loading: boolean;
  declare private _nextIndex: number;

  constructor() {
    super();
    this._items = [];
    this._loading = false;
    this._nextIndex = 0;
    this.selection = { category: DEFAULT_CATEGORY, location: DEFAULT_LOCATION };
  }

  connectedCallback() {
    super.connectedCallback();
    this._fetchFeatured();
  }

  updated(changed: Map<string, unknown>) {
    if (changed.has('selection') && changed.get('selection') !== undefined) {
      this._items = [];
      this._nextIndex = 0;
      this._fetchFeatured();
    }
  }

  private async _fetchFeatured() {
    this._loading = true;
    try {
      const { category, location } = this.selection;
      const data = await getFeatured(this._nextIndex, 4, category, location);

      let previews: OfferingPreview[] = [];
      if (data.series.length > 0) {
        previews = await toPreview(data.series);
      }

      this._items = [...this._items, ...previews];
      this._nextIndex = data.index;
    } catch (e) {
      console.error('Featured offerings error:', e);
    } finally {
      this._loading = false;
    }
  }

  private _handleOfferingSelect(e: CustomEvent<{ id: number; name: string }>) {
    // Navigate to offering profile
    Offering.profileView(e.detail.id);
  }

  static styles = css`
    :host {
      display: block;
      padding: 1rem 0;
    }

    .carousel {
      display: flex;
      gap: 1rem;
      justify-content: center;
      flex-wrap: wrap;
      padding: 0.5rem;
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

    offering-card {
      animation: card-enter 500ms cubic-bezier(0.4, 0, 0.2, 1) both;
    }

    offering-card:nth-child(4n + 1) { animation-delay: 0ms; }
    offering-card:nth-child(4n + 2) { animation-delay: 75ms; }
    offering-card:nth-child(4n + 3) { animation-delay: 150ms; }
    offering-card:nth-child(4n + 4) { animation-delay: 225ms; }

    .loading {
      display: flex;
      justify-content: center;
      padding: 2rem;
    }

    .load-more {
      display: flex;
      justify-content: center;
      padding: 1rem 0;
    }

    .empty {
      text-align: center;
      color: rgba(255,255,255,0.7);
      padding: 2rem;
      font-size: 14px;
    }
  `;

  render() {
    return html`
      ${this._items.length > 0 ? html`
        <div class="carousel">
          ${repeat(this._items, (item) => item.id, (item) => html`
            <offering-card .model="${item}" @offering-select="${this._handleOfferingSelect}"></offering-card>
          `)}
        </div>
      ` : ''}

      ${this._loading ? html`
        <div class="loading">
          <wa-spinner style="font-size: 2rem; --indicator-color: white;"></wa-spinner>
        </div>
      ` : ''}

      ${!this._loading && this._items.length === 0 ? html`
        <div class="empty">No featured offerings available.</div>
      ` : ''}

      ${!this._loading && this._nextIndex > 0 ? html`
        <div class="load-more">
          <wa-button variant="text" @click="${this._fetchFeatured}" style="color: white;">
            <wa-icon slot="prefix" name="arrow-clockwise"></wa-icon>
            Show More
          </wa-button>
        </div>
      ` : ''}
    `;
  }
}

if (!customElements.get('offering-featured')) {
  customElements.define('offering-featured', OfferingFeatured);
}
