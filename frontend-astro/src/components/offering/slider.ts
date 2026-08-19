import { html, css } from 'lit';
import type { OfferingPreview } from '../types.js';
import { toPreview } from '../../service/offering';
import { Offering } from '../../navigation';
import { ListSlider } from '../list/slider';
import './card';

export class OfferingSlider extends ListSlider<OfferingPreview> {
  static get properties() {
    return {
      ...super.properties,
      offeringRefs: { type: Array, attribute: false }
    };
  }

  declare offeringRefs?: any[];
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
    if (changed.has('offeringRefs') && changed.get('offeringRefs') !== undefined) {
      this.reset();
      this.fetchPage();
    }
  }

  protected async fetchPage() {
    if (!this.offeringRefs || this.offeringRefs.length === 0) return;
    this._loading = true;
    try {
      const previews = await toPreview(this.offeringRefs);
      this._items = [...this._items, ...previews];
    } catch (e) {
      console.error('Slider offerings error:', e);
    } finally {
      this._loading = false;
    }
  }

  private _handleOfferingSelect(e: CustomEvent<{ id: number; name: string }>) {
    if (this.companyId) {
      Offering.view(this.companyId, e.detail.id);
    } else {
      Offering.profileView(e.detail.id);
    }
  }

  protected renderItem(item: OfferingPreview) {
    return html`
      <offering-card .model="${item}" @offering-select="${this._handleOfferingSelect}"></offering-card>
    `;
  }

  static styles = [
    ListSlider.styles,
    css`
      offering-card {
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

      offering-card:nth-child(4n + 1) { animation-delay: 0ms; }
      offering-card:nth-child(4n + 2) { animation-delay: 75ms; }
      offering-card:nth-child(4n + 3) { animation-delay: 150ms; }
      offering-card:nth-child(4n + 4) { animation-delay: 225ms; }
    `
  ];
}

if (!customElements.get('offering-slider')) {
  customElements.define('offering-slider', OfferingSlider);
}
