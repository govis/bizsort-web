import { LitElement, html, css } from 'lit';
import { setBasePath } from '@awesome.me/webawesome/dist/utilities/base-path.js';

setBasePath('https://cdn.jsdelivr.net/npm/@awesome.me/webawesome@3.8.0/dist/');

import '@awesome.me/webawesome/dist/components/tab-group/tab-group.js';
import '@awesome.me/webawesome/dist/components/tab/tab.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/input/input.js';

import './category/input';
import './location/input';

/**
 * Search widget for the home page.
 * Uses ported <search-category-input> and <search-location-input>.
 */
export class SearchHome extends LitElement {
  static get properties() {
    return {
      tab: { type: String },
      narrow: { type: Boolean },
      _category: { state: true },
      _location: { state: true }
    };
  }

  declare tab: string;
  declare narrow: boolean;
  declare private _category: string;
  declare private _location: string;

  constructor() {
    super();
    this.tab = 'company';
    this.narrow = false;
    this._category = '';
    this._location = '';
  }

  private _onTabSelect(e: CustomEvent<{ name: string }>) {
    this.tab = e.detail.name;
  }

  private _search() {
    this.dispatchEvent(new CustomEvent('search-submit', {
      composed: true,
      bubbles: true,
      detail: {
        tab: this.tab,
        category: this._category,
        location: this._location
      }
    }));
  }

  private _handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Enter') {
      this._search();
    }
  }

  static styles = css`
    :host {
      display: block;
      flex-shrink: 0;
      width: 100%;
      max-width: 700px;
    }

    .content {
      padding: 0 24px 48px;
      box-sizing: border-box;
      width: 100%;
      background-color: rgba(83, 109, 254, 0.85);
      backdrop-filter: blur(12px);
      border-radius: 16px;
      position: relative;
      box-shadow: 0 4px 24px rgba(0,0,0,0.15);
    }

    /* Tab overrides */
    wa-tab-group {
      --indicator-color: white;
      --track-color: transparent;
      margin-bottom: 1rem;
    }

    wa-tab {
      color: rgba(255,255,255,0.7);
    }

    wa-tab::part(base) {
      padding: 0.75rem 1.25rem;
      font-weight: 500;
      font-size: 14px;
    }

    wa-tab[active] {
      color: white;
    }

    .search-inputs {
      display: flex;
      gap: 0.75rem;
    }

    :host([narrow]) .search-inputs {
      flex-direction: column;
    }

    .search-inputs search-category-input,
    .search-inputs search-location-input {
      flex: 1;
    }

    search-category-input::part(base),
    search-location-input::part(base) {
      background: rgba(255,255,255,0.15);
      border-color: rgba(255,255,255,0.3);
      color: white;
      border-radius: 8px;
    }

    search-category-input::part(input),
    search-location-input::part(input) {
      color: white;
    }

    search-category-input::part(form-control-label),
    search-location-input::part(form-control-label) {
      color: rgba(255,255,255,0.9);
      font-size: 13px;
    }

    .search-fab {
      position: absolute;
      bottom: -24px;
      left: 50%;
      transform: translateX(-50%);
      z-index: 10;
    }

    .search-fab wa-button::part(base) {
      border-radius: 50%;
      width: 56px;
      height: 56px;
      background: #ff6f00;
      color: white;
      box-shadow: 0 4px 12px rgba(255, 111, 0, 0.4);
      transition: transform 0.2s ease, box-shadow 0.2s ease;
    }

    .search-fab wa-button:hover::part(base) {
      transform: scale(1.1);
      box-shadow: 0 6px 20px rgba(255, 111, 0, 0.5);
    }

    ::slotted(*) {
      margin-top: 1rem;
    }
  `;

  render() {
    return html`
      <div class="content">
        <wa-tab-group @wa-tab-show="${this._onTabSelect}">
          <wa-tab slot="nav" panel="company" ?active="${this.tab === 'company'}">Companies</wa-tab>
          <wa-tab slot="nav" panel="product" ?active="${this.tab === 'product'}">Offerings</wa-tab>
          <wa-tab slot="nav" panel="project" ?active="${this.tab === 'project'}">Projects</wa-tab>
          <wa-tab slot="nav" panel="job" ?active="${this.tab === 'job'}">Jobs</wa-tab>
        </wa-tab-group>

        <div class="search-inputs" @keydown="${this._handleKeydown}">
          <search-category-input
            placeholder="Category, keyword, or name"
            label="What"
            @category-selected="${(e: CustomEvent) => this._category = e.detail ? e.detail.name : ''}"
            @category-cleared="${() => this._category = ''}"
          ></search-category-input>
          
          <search-location-input
            placeholder="City, province, or postal code"
            label="Where"
            geoMode
            @location-selected="${(e: CustomEvent) => this._location = e.detail ? e.detail.name : ''}"
            @location-cleared="${() => this._location = ''}"
          ></search-location-input>
        </div>

        <slot></slot>

        <div class="search-fab">
          <wa-button variant="default" is-icon-button @click="${this._search}">
            <wa-icon name="search" style="font-size: 1.5rem;"></wa-icon>
          </wa-button>
        </div>
      </div>
    `;
  }
}

if (!customElements.get('search-home')) {
  customElements.define('search-home', SearchHome);
}
