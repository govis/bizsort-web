import { LitElement, html, css } from 'lit';
import type { Category, Location } from '../../types.js';

import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';

export class SearchCategoryMenu extends LitElement {
  static get properties() {
    return {
      category: { type: Object },
      location: { type: Object }
    };
  }

  declare category?: Category;
  declare location?: Location;

  static styles = css`
    :host { display: inline-block; }
    wa-button::part(base) {
      color: var(--wa-color-neutral-600);
    }
  `;

  private _handleAction(e: Event, type: 'in' | 'near') {
    e.preventDefault();
    // In a real implementation, this would trigger Next.js routing with the search token
    console.log(`Searching category ${this.category?.name} ${type} location`);
    
    // Close dropdown
    const dropdown = this.shadowRoot?.querySelector('wa-dropdown');
    if (dropdown) {
      (dropdown as any).open = false;
    }
  }

  render() {
    if (!this.category || !this.location) {
      // Fallback if data isn't loaded yet
      return html`
        <wa-dropdown placement="bottom-end">
          <wa-button slot="trigger" variant="text" size="small" is-icon-button>
            <wa-icon name="caret-down-fill"></wa-icon>
          </wa-button>
          <wa-dropdown-item>Search Category</wa-dropdown-item>
        </wa-dropdown>
      `;
    }

    const city = this.location.address?.split(',')[0] || 'City'; // Basic extraction from full address string
    const postalCodeMatch = this.location.address?.match(/[A-Z]\d[A-Z] \d[A-Z]\d/i);
    const postalCode = postalCodeMatch ? postalCodeMatch[0] : null;

    return html`
      <wa-dropdown placement="bottom-end">
        <wa-button slot="trigger" variant="text" size="small" is-icon-button>
          <wa-icon name="caret-down-fill"></wa-icon>
        </wa-button>
        
        <wa-dropdown-item @click="${(e: Event) => this._handleAction(e, 'in')}">
          in ${city}
        </wa-dropdown-item>
        
        ${(postalCode || city) && this.location.geoLocation ? html`
          <wa-dropdown-item @click="${(e: Event) => this._handleAction(e, 'near')}">
            near ${postalCode || city}
          </wa-dropdown-item>
        ` : ''}
      </wa-dropdown>
    `;
  }
}

if (!customElements.get('search-category-menu')) {
  customElements.define('search-category-menu', SearchCategoryMenu);
}
