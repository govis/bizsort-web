import { LitElement, html, css } from 'lit';

import '@awesome.me/webawesome/dist/components/tab-group/tab-group.js';
import '@awesome.me/webawesome/dist/components/tab/tab.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/input/input.js';

import './category/input';
import './location/input';

import { SearchHome$ } from '../../viewmodel/search/home';
import { IViewAdapter } from '../../viewmodel';
import { SearchCategoryInput } from './category/input';
import { SearchLocationInput } from './location/input';
import { Navigation, Company, Product } from '../../navigation';

/**
 * Search widget for the home page.
 * Uses ported <search-category-input> and <search-location-input>.
 */
export class SearchHome extends LitElement implements IViewAdapter {
  static get properties() {
    return {
      tab: { type: String },
      narrow: { type: Boolean },
      categoryId: { type: Number, attribute: 'category-id' },
      locationId: { type: Number, attribute: 'location-id' },
      searchQuery: { type: String, attribute: 'search-query' },
      searchNear: { type: String, attribute: 'search-near' }
    };
  }

  declare tab: string;
  declare narrow: boolean;
  declare categoryId?: number;
  declare locationId?: number;
  declare searchQuery?: string;
  declare searchNear?: string;
  model: SearchHome$;

  constructor() {
    super();
    this.tab = 'company';
    this.narrow = false;
    this.model = new SearchHome$(this);
  }
  modelUpdated(props: string[]) {
    // Re-render when viewmodel selection changes
    if (props.includes('selection')) {
        this.requestUpdate();
    }
  }

  willUpdate(changedProperties: Map<string, any>) {
    if (changedProperties.has('categoryId') || 
        changedProperties.has('locationId') || 
        changedProperties.has('searchQuery') || 
        changedProperties.has('searchNear')) {
        
        let nearObj = undefined;
        try {
            if (this.searchNear) {
                nearObj = JSON.parse(this.searchNear);
            }
        } catch(e) {}

        this.model.loadSelection({
            category: this.categoryId || 0,
            location: this.locationId || 0,
            query: this.searchQuery,
            near: nearObj
        });
    }
  }

  firstUpdated() {
    const category = this.shadowRoot?.querySelector('search-category-input') as SearchCategoryInput;
    const location = this.shadowRoot?.querySelector('search-location-input') as SearchLocationInput;
    if (category && location) {
      this.model.attachInputs(category.model, location.model);
    }
  }

  private _onTabSelect(e: CustomEvent<{ name: string }>) {
    const targetTab = e.detail.name;
    if (targetTab === this.tab) return;
    
    // Flush current inputs to model selection before navigating (bypass 300ms debouncer!)
    if (this.model) {
      const catInput = this.shadowRoot?.querySelector('search-category-input') as any;
      if (catInput?.inputElement) {
        catInput.model.text = catInput.inputElement.value;
      }

      const locInput = this.shadowRoot?.querySelector('search-location-input') as any;
      if (locInput?.inputElement) {
        locInput.model.text = locInput.inputElement.value;
      }

      this.model.reflectSelection();
    }
    
    // Construct params
    const selection = this.model?.selection;
    const params: any = {};
    if (selection) {
      if (selection.category) params.categoryId = selection.category;
      if (selection.location) params.locationId = selection.location;
      if (selection.query) params.searchQuery = selection.query;
      if (selection.near) params.searchNear = selection.near;
    }
    
    switch (targetTab) {
        case 'company': 
            Company.home(params); 
            break;
        case 'product': 
            Product.home(params); 
            break;
        case 'project': 
            Navigation.go('/project', params); 
            break;
        case 'job': 
            Navigation.go('/job', params); 
            break;
    }
  }

  private async _search() {
    if (this.model && this.model.validate()) {
      const selection = this.model.selection;
      this.dispatchEvent(new CustomEvent('search-submit', {
        composed: true,
        bubbles: true,
        detail: {
          tab: this.tab,
          category: selection ? selection.category : 0,
          location: selection ? selection.location : 0,
          query: selection ? selection.query : undefined,
          near: selection ? selection.near : undefined
        }
      }));
    } else {
      // Validation failed. Wait for Lit to flush the custom validity states to the DOM
      const category = this.shadowRoot?.querySelector('search-category-input') as any;
      const location = this.shadowRoot?.querySelector('search-location-input') as any;
      
      if (category) await category.updateComplete;
      if (location) await location.updateComplete;

      const catInput = category?.shadowRoot?.querySelector('wa-input');
      const locInput = location?.shadowRoot?.querySelector('wa-input');

      // The browser natively only supports one validation popup at a time.
      // We explicitly report the first invalid one so it doesn't get overwritten.
      if (catInput && !catInput.checkValidity()) {
          catInput.reportValidity();
      } else if (locInput && !locInput.checkValidity()) {
          locInput.reportValidity();
      }
    }
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
      --search-home-background: rgba(83, 109, 254, 0.85);
      
      /* Cascade WebAwesome input variables to children (mimicking legacy pattern) */
      --wa-form-control-background-color: rgba(255,255,255,0.15);
      --wa-color-neutral-fill-quiet: rgba(255,255,255,0.15);
      
      --wa-form-control-value-color: white;
      --wa-form-control-placeholder-color: rgba(255,255,255,0.9);
      --wa-color-neutral-on-quiet: white; 
      
      --wa-form-control-border-color: rgba(255,255,255,0.3);
      --wa-form-control-label-color: rgba(255,255,255,0.9);

      padding: 0 24px 48px;
      box-sizing: border-box;
      width: 100%;
      background-color: var(--search-home-background);
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

    wa-tab-group::part(tabs) {
      justify-content: center;
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

    .search-fab {
      position: absolute;
      bottom: -24px;
      left: 50%;
      transform: translateX(-50%);
      z-index: 10;
    }

    .search-fab wa-button {
      --wa-color-brand-fill-loud: var(--color-accent1, #e040fb);
      --wa-color-brand-on-loud: white;
      --wa-color-brand-border-loud: transparent;
    }

    .search-fab wa-button::part(base) {
      border-radius: 50%;
      width: 56px;
      height: 56px;
      box-shadow: 0 4px 12px rgba(224, 64, 251, 0.4);
      transition: transform 0.2s ease, box-shadow 0.2s ease;
    }

    .search-fab wa-button:hover::part(base) {
      transform: scale(1.1);
      box-shadow: 0 6px 20px rgba(224, 64, 251, 0.5);
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
            @category-selected="${(e: CustomEvent) => this.categoryId = e.detail ? e.detail.id : 0}"
            @category-cleared="${() => this.categoryId = 0}"
          ></search-category-input>
          
          <search-location-input
            placeholder="City, province, or postal code"
            label="Where"
            @location-selected="${(e: CustomEvent) => this.locationId = e.detail ? e.detail.id : 0}"
            @location-cleared="${() => this.locationId = 0}"
          ></search-location-input>
        </div>

        <slot></slot>

        <div class="search-fab">
          <wa-button variant="brand" is-icon-button @click="${this._search}">
            <wa-icon name="search" style="font-size: 1.2rem;"></wa-icon>
          </wa-button>
        </div>
      </div>
    `;
  }
}

if (!customElements.get('search-home')) {
  customElements.define('search-home', SearchHome);
}
