import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import { SearchHome$ } from '../../viewmodel/search/home';
import { IViewAdapter } from '../../viewmodel';
import { Navigation, Company, Product } from '../../navigation';

import './category/input';
import './location/input';
import { SearchCategoryInput } from './category/input';
import { SearchLocationInput } from './location/input';

import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/button/button.js';

@customElement('search-header')
export class SearchHeader extends LitElement implements IViewAdapter {
  @property({ type: String, attribute: 'entity-type' })
  declare entityType: string;

  @property({ type: Number, attribute: 'category-id' })
  declare categoryId?: number;

  @property({ type: Number, attribute: 'location-id' })
  declare locationId?: number;

  @property({ type: String, attribute: 'search-query' })
  declare searchQuery?: string;

  @property({ type: String, attribute: 'search-near' })
  declare searchNear?: string;

  @state()
  declare _geoMode: boolean;

  model: SearchHome$;

  public modelUpdated(props: string[] = []) {
    this.requestUpdate();
    if (props.includes('selection')) {
        this._syncUrlState();
    }
  }

  private _syncUrlState() {
    if (!this.model || !this.model.selection) return;
    try {
        const url = new URL(window.location.href);
        const sel = this.model.selection;
        
        if (sel.category) url.searchParams.set('categoryId', sel.category.toString());
        else url.searchParams.delete('categoryId');
        
        if (sel.location) url.searchParams.set('locationId', sel.location.toString());
        else url.searchParams.delete('locationId');
        
        if (sel.query) url.searchParams.set('searchQuery', sel.query);
        else url.searchParams.delete('searchQuery');
        
        if (sel.near) url.searchParams.set('searchNear', JSON.stringify(sel.near));
        else url.searchParams.delete('searchNear');
        
        window.history.replaceState(null, '', url.toString());
    } catch (e) {
        // Ignore URL parsing errors during SSR or edge cases
    }
  }

  constructor() {
    super();
    this.entityType = 'company';
    this._geoMode = false;
    this.model = new SearchHome$(this);
  }

  static styles = css`
    :host {
      display: block;
      height: 72px;
      width: 100%;
    }

    .header-top {
      display: flex;
      flex-direction: row;
      align-items: center;
      height: 100%;
      padding: 0 1rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .logo {
      display: flex;
      align-items: center;
      cursor: pointer;
      text-decoration: none;
      color: white;
      font-weight: bold;
      font-size: 1.5rem;
      margin-right: 2rem;
    }

    .search-inputs {
      flex: 1;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      max-width: 800px;
    }

    search-category-input,
    search-location-input {
      flex: 1;
      width: 100%;
      margin-bottom: 12px; /* Push inputs higher while keeping search button centered */
      
      /* Cascade WebAwesome input variables to children (mimicking legacy pattern) */
      --wa-form-control-background-color: rgba(255,255,255,0.15);
      --wa-color-neutral-fill-quiet: rgba(255,255,255,0.15);
      
      --wa-form-control-value-color: white;
      --wa-form-control-placeholder-color: rgba(255,255,255,0.9);
      --wa-color-neutral-on-quiet: white; 
      
      --wa-form-control-border-color: rgba(255,255,255,0.3);
      --wa-form-control-label-color: rgba(255,255,255,0.9);
    }

    .geo-action {
      /* wa-button defaults to size="medium" which is exactly 40px tall/wide when used as an icon button */
      margin-right: 0.25rem;
      margin-bottom: 12px; /* Match input offset */
      box-shadow: 0 4px 10px rgba(0, 0, 0, 0.15);
      border-radius: var(--wa-border-radius-circle, 50%);
    }

    .geo-action[variant="neutral"] {
      --wa-color-neutral-fill-loud: var(--primary-theme-color, #448aff);
      --wa-color-neutral-on-loud: white;
      --wa-color-neutral-border-loud: transparent;
      opacity: 0.8;
    }

    .geo-action[variant="brand"] {
      --wa-color-brand-fill-loud: var(--primary-theme-color, #448aff);
      --wa-color-brand-on-loud: white;
      --wa-color-brand-border-loud: transparent;
      opacity: 1;
    }

    .geo-action wa-icon {
      font-size: 20px;
    }

    .search-button {
      margin-left: 8px;
      --wa-color-brand-fill-loud: var(--primary-theme-color, #448aff);
      --wa-color-brand-on-loud: white;
      --wa-color-brand-border-loud: transparent;
      border-radius: var(--wa-border-radius-circle, 50%);
      box-shadow: 0 4px 10px rgba(0, 0, 0, 0.15); /* Matched to geoMode button */
      transition: transform 0.2s ease, box-shadow 0.2s ease;
    }
    
    .search-button:hover {
      transform: scale(1.05);
      box-shadow: 0 6px 14px rgba(0, 0, 0, 0.2);
    }
  `;

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

  private _handleHome() {
    if (this.entityType.toLowerCase() === 'product') {
      Product.home();
    } else {
      Company.home();
    }
  }

  private async _handleSearch() {
    // Flush current inputs to model selection before navigating (bypass debouncer!)
    const catInput = this.shadowRoot?.querySelector('search-category-input') as any;
    if (catInput?.inputElement) {
      catInput.model.text = catInput.inputElement.value;
    }

    const locInput = this.shadowRoot?.querySelector('search-location-input') as any;
    if (locInput?.inputElement) {
      locInput.model.text = locInput.inputElement.value;
    }

    if (this.model && this.model.validate()) {
      const selection = this.model.selection;
      
      const params: any = {};
      if (selection.category > 0) params.categoryId = selection.category;
      if (selection.query) params.searchQuery = selection.query;
      
      if (selection.near) {
          params.searchNear = selection.near;
      } else if (selection.location > 0) {
          params.locationId = selection.location;
      }

      if (this.entityType.toLowerCase() === 'product') {
         Navigation.go(Product.searchPage, params);
      } else {
         Navigation.go(Company.searchPage, params);
      }
    } else {
      // Show validation errors
      if (catInput) await catInput.updateComplete;
      if (locInput) await locInput.updateComplete;

      const catWaInput = catInput?.shadowRoot?.querySelector('wa-input');
      const locWaInput = locInput?.shadowRoot?.querySelector('wa-input');

      if (catWaInput && !catWaInput.checkValidity()) {
        catWaInput.reportValidity();
      } else if (locWaInput && !locWaInput.checkValidity()) {
        locWaInput.reportValidity();
      }
    }
  }

  render() {
    return html`
      <div class="header-top">
        <a class="logo" @click=${this._handleHome}>
          bizSORT
        </a>
        
        <div class="search-inputs">
          <search-category-input 
            id="category" 
            placeholder="What"
            .categoryId=${this.categoryId}
            .searchQuery=${this.searchQuery}
          ></search-category-input>
          
          <wa-button 
            variant="${this._geoMode ? 'brand' : 'neutral'}"
            is-icon-button
            pill
            class="geo-action"
            @click="${() => {
              this._geoMode = !this._geoMode;
              const locInput = this.shadowRoot?.querySelector('search-location-input') as any;
              if (locInput) locInput.geoMode = this._geoMode;
            }}"
          >
            <wa-icon library="bizsrt" name="${this._geoMode ? 'place' : 'location-off'}"></wa-icon>
          </wa-button>
          
          <search-location-input 
            id="location" 
            placeholder="Where"
            .locationId=${this.locationId}
            .searchNear=${this.searchNear}
            @geomodeChange=${(e: CustomEvent) => this._geoMode = e.detail.value}
          ></search-location-input>
          
          <wa-button class="search-button" is-icon-button pill variant="brand" size="large" @click=${this._handleSearch}>
            <wa-icon name="search"></wa-icon>
          </wa-button>
        </div>
      </div>
    `;
  }
}
