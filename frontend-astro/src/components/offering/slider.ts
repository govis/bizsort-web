import { html, css } from 'lit';
import { property } from 'lit/decorators.js';
import type { OfferingPreview } from '../types.js';
import { toPreview } from '../../service/offering';
import { Offering } from '../../navigation';
import { ListSlider } from '../list/slider';
import './card';

export class OfferingSlider extends ListSlider<OfferingPreview> {
  @property({ type: Array, attribute: false })
  declare offeringRefs?: any[];
  
  declare private _displayOptions: any;

  constructor() {
    super();
    this._displayOptions = { company: false };
  }

  willUpdate(changed: Map<string, unknown>) {
    if (changed.has('offeringRefs') && changed.get('offeringRefs') !== undefined) {
      this.reset();
      this.fetchPage();
    } else if (changed.has('companyId') && this.companyId) {
      // If companyId was just initialized or changed, fetch!
      this.reset();
      this.fetchPage();
    }
  }

  protected async fetchPage() {
    this._loading = true;
    try {
      let refs = this.offeringRefs;

      // If offeringRefs is empty but we have a companyId, fetch the list from the company service
      if ((!refs || refs.length === 0) && this.companyId) {
        const { getCompanyFeaturedOfferings } = await import('../../service/company.js');
        const sliceOutput = await getCompanyFeaturedOfferings(this.companyId, this._nextIndex, 12);
        refs = sliceOutput.series;
        this._nextIndex = sliceOutput.index;
      }

      if (!refs || refs.length === 0) {
        return;
      }

      const previews = await toPreview(refs);
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
