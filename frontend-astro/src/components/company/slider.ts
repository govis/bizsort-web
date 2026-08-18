import { html, css } from 'lit';
import type { CompanyPreview } from '../types.js';
import { getAffiliations, toPreview } from '../../service/company';
import { Company } from '../../navigation';
import { ListSlider } from '../list/slider';
import './card';

export class CompanySlider extends ListSlider<CompanyPreview> {
  connectedCallback() {
    super.connectedCallback();
    this.fetchPage();
  }

  willUpdate(changed: Map<string, unknown>) {
    if (changed.has('companyId') && changed.get('companyId') !== undefined) {
      this.reset();
      this.fetchPage();
    }
  }

  protected async fetchPage() {
    if (!this.companyId) return;
    this._loading = true;
    try {
      const data = await getAffiliations(this.companyId, this._nextIndex, 4);

      let previews: CompanyPreview[] = [];
      if (data.series.length > 0) {
        previews = await toPreview(data.series);
      }

      this._items = [...this._items, ...previews];
      this._nextIndex = data.index;
    } catch (e) {
      console.error('Company slider error:', e);
    } finally {
      this._loading = false;
    }
  }

  private _handleCompanySelect(e: CustomEvent<{ id: number; name: string }>) {
    Company.profileView(e.detail.id);
  }

  protected renderItem(item: CompanyPreview) {
    return html`
      <company-card .model="${item}" @company-select="${this._handleCompanySelect}"></company-card>
    `;
  }

  static styles = [
    ListSlider.styles,
    css`
      company-card {
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

      company-card:nth-child(4n + 1) { animation-delay: 0ms; }
      company-card:nth-child(4n + 2) { animation-delay: 75ms; }
      company-card:nth-child(4n + 3) { animation-delay: 150ms; }
      company-card:nth-child(4n + 4) { animation-delay: 225ms; }
    `
  ];
}

if (!customElements.get('company-slider')) {
  customElements.define('company-slider', CompanySlider);
}
